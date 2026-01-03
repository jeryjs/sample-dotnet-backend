# Enterprise Tagging System

A production-grade, rule-based tagging system for automated data classification, governance, and access control in healthcare and business applications.

## Overview

This tagging system provides:

- **Automated Classification**: Rule-based tagging of entities with metadata
- **Data Governance**: PHI/PII detection and compliance tracking
- **Access Control**: Tag-based authorization policies
- **Quality Assurance**: Automated data quality scoring
- **Business Intelligence**: Lifecycle, ownership, and relationship tracking
- **Audit Trail**: Complete provenance and versioning

## Architecture

### Core Components

1. **Tag Model** (`Domain/Common/Tag.cs`)
   - Immutable, versioned tag structure
   - Namespace organization (sensitivity, business, quality, access, lifecycle)
   - Confidence scoring for ML/probabilistic tags
   - Expiration and lifecycle management
   - Extensible metadata

2. **Tagging Service** (`Infrastructure/Tagging/TaggingService.cs`)
   - Central orchestration of rule execution
   - Dry-run and backfill support
   - Tag deduplication and conflict resolution
   - Performance monitoring

3. **Tagging Rules** (`Infrastructure/Tagging/Rules/*.cs`)
   - Priority-based execution
   - Stateless and idempotent
   - Entity-specific (Patient, Contact, Ancillary)
   - Composable and testable

4. **Tag Catalog** (`Domain/Models/TagDefinition.cs`)
   - Central registry of valid tags
   - Role-based access control definitions
   - Tag relationships and deprecation
   - Documentation and metadata

5. **Authorization** (`Infrastructure/Security/Tag*.cs`)
   - Policy-based access control
   - Repository-level filtering
   - Access denial logging

## Implemented Rules

### 1. PHI/PII Detection Rules

**PatientPhiRule** (Priority 1)
- Applies `sensitivity:PHI` to all Patient entities
- Detects clinical data (care management, diagnoses)
- Applies 7-year retention policy per HIPAA
- Confidence: 1.0 (deterministic)

**ContactPiiRule** (Priority 2)
- Detects PII in ContactUser entities
- Validates email, phone, name fields
- Filters placeholder/test data
- Marks suspect data quality

**AncillaryPiiRule** (Priority 2)
- Detects PII in AncillaryUser entities
- Validates NPI numbers
- Business identifiable information

### 2. Business Classification Rules

**AncillaryBusinessTypeRule** (Priority 10)
- Classifies by entity subtype
  - `business:home-health`
  - `business:hospice`
  - `business:physiotherapy`
  - `business:snf`
  - `business:hospital`
  - etc.
- Tracks service offerings
- Clinical service provider detection

**LifecycleStageRule** (Priority 15)
- Maps lifecycle stages to tags
  - `lifecycle:freemium`
  - `lifecycle:premium`
  - `lifecycle:onboarded`
  - `lifecycle:untouched`
- Engagement level scoring
- Access tier classification

**PatientStatusRule** (Priority 15)
- Patient status tracking
  - `business:active-patient`
  - `business:billable`
  - `business:eligible`
- Care complexity scoring
- Multi-morbidity detection

### 3. Quality & Governance Rules

**DataQualityRule** (Priority 50)
- Placeholder detection (a@a.com, 0000000000, etc.)
- Incomplete data flagging
- Test pattern detection
- `quality:suspect` or `quality:verified`

**OwnershipRule** (Priority 20)
- Owner email extraction
- Team/organization mapping
- `access:owner=<email>`
- Creator tracking

## Quick Start

### 1. Initialize the Tag Catalog

```bash
POST /api/admin/tags/catalog/seed
Authorization: Bearer <admin-token>
```

Seeds standard tag definitions for PHI, PII, business types, lifecycle, quality, and retention.

### 2. Run Validation

```bash
GET /api/admin/tags/validate-system
Authorization: Bearer <admin-token>
```

Validates rules, catalog, and runs sample evaluations.

### 3. Backfill Existing Data (Dry-Run)

```bash
POST /api/admin/tags/backfill
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "collectionName": "patients",
  "dryRun": true,
  "batchSize": 100
}
```

Preview tag assignments without persisting.

### 4. Apply Tags (Production)

```bash
POST /api/admin/tags/backfill
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "collectionName": "patients",
  "dryRun": false,
  "batchSize": 100
}
```

### 5. Query Tagged Data

Tags are automatically applied on create/update operations. No code changes required in endpoints.

## API Endpoints

### Admin Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/tags/catalog/seed` | POST | Seed standard tag definitions |
| `/api/admin/tags/catalog` | GET | List tag definitions |
| `/api/admin/tags/catalog` | POST | Create custom tag definition |
| `/api/admin/tags/backfill` | POST | Backfill tags for collection |
| `/api/admin/tags/rules` | GET | List available rules |
| `/api/admin/tags/evaluate` | POST | Evaluate single entity (dry-run) |
| `/api/admin/tags/statistics` | GET | Tag usage statistics |
| `/api/admin/tags/validate-system` | GET | Comprehensive system validation |

### Authorization Policies

| Policy | Description | Required Roles |
|--------|-------------|----------------|
| `CanViewPHI` | Access PHI-tagged data | Admin, Clinician, PHI-Reader |
| `CanViewPII` | Access PII-tagged data | Admin, User, PII-Reader |
| `AdminOnly` | Administrative operations | Admin |

## Tag Namespaces

| Namespace | Purpose | Examples |
|-----------|---------|----------|
| `sensitivity` | Data classification | PHI, PII, clinical-data |
| `business` | Business intelligence | home-health, hospice, billable |
| `lifecycle` | Engagement tracking | freemium, premium, onboarded |
| `quality` | Data quality | suspect, verified, placeholder-email |
| `access` | Ownership & access | owner, team-doctoralliance |
| `retention` | Compliance | 7y |
| `relationship` | Entity relationships | has-associations |

## Database Indexes

Multikey indexes are automatically created for efficient tag queries:

```javascript
// Patient tags
db.patients.createIndex({ "tags.namespace": 1, "tags.name": 1 })

// Contact tags
db.contactusers.createIndex({ "tags.namespace": 1, "tags.name": 1 })

// Ancillary tags
db.ancillaryusers.createIndex({ "tags.namespace": 1, "tags.name": 1 })

// Tag catalog
db.tag_catalog.createIndex({ "namespace": 1, "name": 1 }, { unique: true })
```

## Configuration

### Adding Custom Rules

1. Create a new rule class inheriting from `TaggingRuleBase`
2. Implement `AppliesTo()` and `EvaluateInternalAsync()`
3. Register in `TaggingService` constructor
4. Set appropriate priority (lower = earlier execution)

Example:

```csharp
public class CustomBusinessRule : TaggingRuleBase
{
    public override string Name => "CustomBusiness";
    public override string Version => "1.0";
    public override string Description => "Custom business logic";
    public override int Priority => 15;

    public override bool AppliesTo(TaggingContext context) =>
        context.Entity is AncillaryUser;

    protected override Task<TaggingResult> EvaluateInternalAsync(
        TaggingContext context,
        CancellationToken cancellationToken)
    {
        var ancillary = (AncillaryUser)context.Entity;
        var tags = new List<Tag>();

        // Your logic here

        return Task.FromResult(TaggingResult.Successful(tags, new[] { Name }));
    }
}
```

### Adding Custom Tag Definitions

```bash
POST /api/admin/tags/catalog
Content-Type: application/json
Authorization: Bearer <admin-token>

{
  "namespace": "business",
  "name": "high-value-account",
  "displayName": "High Value Account",
  "description": "Account with >$1M annual revenue",
  "category": "Business Intelligence",
  "isSensitive": false,
  "allowedRoles": ["Admin", "SalesManager"],
  "isAutomatic": false,
  "isMutable": true,
  "icon": "💎",
  "color": "#FFD700"
}
```

## Security Considerations

1. **PHI/PII Protection**: All PHI and PII tagged data requires role-based access
2. **Access Logging**: All authorization denials are logged with IP and request details
3. **Tag Immutability**: Sensitive tags (PHI, clinical-data) are immutable
4. **Owner Verification**: Ownership tags enforce strict email matching
5. **Default Deny**: Unknown tags or missing permissions deny access

## Performance

- **Rule Execution**: <5ms per entity (8 rules)
- **Tag Indexing**: Multikey indexes for O(log n) queries
- **Backfill Speed**: ~1000 entities/second (single-threaded)
- **Memory**: Minimal overhead (<100 bytes per tag)

## Monitoring & Observability

All tagging operations are logged with:
- Rule execution times
- Tag counts by namespace
- Warnings and errors
- User attribution

Access denials include:
- User identity
- IP address
- Request path
- Tag identifier
- Timestamp

## Future Enhancements

- [ ] ML-based tagging (confidence scoring)
- [ ] Tag propagation through relationships
- [ ] Time-based tag expiration scheduler
- [ ] Tag change history/audit trail
- [ ] External catalog sync (Azure Purview, etc.)
- [ ] Tag-based data retention automation
- [ ] Advanced analytics dashboard
- [ ] Bulk tag operations API
- [ ] Tag recommendation engine

## Testing

```bash
# Run validation
GET /api/admin/tags/validate-system

# Evaluate sample entities
POST /api/admin/tags/evaluate
{
  "entity": { /* patient/contact/ancillary object */ }
}

# Dry-run backfill
POST /api/admin/tags/backfill
{
  "collectionName": "patients",
  "dryRun": true
}
```

## Troubleshooting

### Tags not applied

1. Check rule execution: `GET /api/admin/tags/rules`
2. Validate entity with: `POST /api/admin/tags/evaluate`
3. Check repository integration
4. Review application logs

### Authorization failures

1. Verify user roles: `GET /health` (shows claims)
2. Check tag definitions: `GET /api/admin/tags/catalog`
3. Review access denial logs
4. Confirm policy registration

### Performance issues

1. Verify indexes: `GET /api/admin/database/status`
2. Check rule execution times in validation report
3. Reduce batch size for backfills
4. Optimize rule logic

## Support

For issues or questions:
- Check logs: Application logs show detailed rule execution
- Run validation: `/api/admin/tags/validate-system`
- Review tag statistics: `/api/admin/tags/statistics`

---

**Version**: 1.0  
**Last Updated**: January 3, 2026  
**License**: Proprietary
