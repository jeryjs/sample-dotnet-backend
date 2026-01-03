# Tagging System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLIENT / API CONSUMER                            │
└────────────┬────────────────────────────────────────────────────────────┘
             │
             │ HTTP Request
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           FASTENDPOINTS                                  │
│  ┌────────────────────┐  ┌──────────────────┐  ┌────────────────────┐  │
│  │  Patient Endpoints │  │ Contact Endpoints│  │ Ancillary Endpoints│  │
│  │  - Create         │  │  - Create        │  │  - Create          │  │
│  │  - Update         │  │  - Update        │  │  - Update          │  │
│  │  - Get/Search     │  │  - Get/Search    │  │  - Get/Search      │  │
│  └─────────┬──────────┘  └────────┬─────────┘  └──────────┬─────────┘  │
└────────────┼─────────────────────┼────────────────────────┼─────────────┘
             │                     │                        │
             │ Policies:           │                        │
             │ - CanViewPHI        │                        │
             │ - CanViewPII        │                        │
             ▼                     ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      AUTHORIZATION LAYER                                 │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              TagAuthorizationHandler                                │ │
│  │  - Check sensitivity tags (PHI, PII)                                │ │
│  │  - Validate user roles                                              │ │
│  │  - Enforce access tags (owner, team-*)                              │ │
│  │  - Log access denials                                               │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │              TagAccessFilter                                        │ │
│  │  - Repository-level filtering                                       │ │
│  │  - Entity sanitization                                              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────┬────────────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        REPOSITORY LAYER                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐      │
│  │PatientRepository │  │ContactRepository │  │AncillaryRepository│      │
│  │                  │  │                  │  │                   │      │
│  │ Create() ───────────────────┐         │  │                   │      │
│  │ Update() ───────────────────┤         │  │                   │      │
│  │ Get*()   ───────────────────┤         │  │                   │      │
│  └──────────────────┘         ▼         └──────────────────┘      │
│                    ┌──────────────────────┐                            │
│                    │  ITaggingService     │                            │
│                    │  ApplyTagsAsync()    │◄───── Auto-invoked on     │
│                    └──────────┬───────────┘       create/update        │
└───────────────────────────────┼────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       TAGGING SERVICE                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                     TaggingService                                  │ │
│  │  ┌──────────────────────────────────────────────────────────────┐  │ │
│  │  │  EvaluateAsync()                                              │  │ │
│  │  │  - Iterate through rules by priority                          │  │ │
│  │  │  - Collect tags from each rule                                │  │ │
│  │  │  - Deduplicate by identifier                                  │  │ │
│  │  │  - Return TaggingResult                                       │  │ │
│  │  └──────────────────────────────────────────────────────────────┘  │ │
│  │  ┌──────────────────────────────────────────────────────────────┐  │ │
│  │  │  ApplyTagsAsync()                                             │  │ │
│  │  │  - Evaluate entity                                            │  │ │
│  │  │  - Merge with existing tags                                   │  │ │
│  │  │  - Remove expired tags                                        │  │ │
│  │  │  - Update entity                                              │  │ │
│  │  └──────────────────────────────────────────────────────────────┘  │ │
│  │  ┌──────────────────────────────────────────────────────────────┐  │ │
│  │  │  BackfillCollectionAsync()                                    │  │ │
│  │  │  - Batch process entities                                     │  │ │
│  │  │  - Track statistics                                           │  │ │
│  │  │  - Support dry-run                                            │  │ │
│  │  └──────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                │                                         │
│                                ▼                                         │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                        RULE ENGINE                                  │ │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐       │ │
│  │  │PatientPhiRule  │  │ContactPiiRule  │  │AncillaryPiiRule│       │ │
│  │  │Priority: 1     │  │Priority: 2     │  │Priority: 2     │       │ │
│  │  └────────────────┘  └────────────────┘  └────────────────┘       │ │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐       │ │
│  │  │BusinessTypeRule│  │LifecycleRule   │  │PatientStatusRule│      │ │
│  │  │Priority: 10    │  │Priority: 15    │  │Priority: 15    │       │ │
│  │  └────────────────┘  └────────────────┘  └────────────────┘       │ │
│  │  ┌────────────────┐  ┌────────────────┐                            │ │
│  │  │OwnershipRule   │  │DataQualityRule │                            │ │
│  │  │Priority: 20    │  │Priority: 50    │                            │ │
│  │  └────────────────┘  └────────────────┘                            │ │
│  │                                                                      │ │
│  │  Each rule implements:                                              │ │
│  │  - AppliesTo(context): bool                                         │ │
│  │  - EvaluateAsync(context): TaggingResult                            │ │
│  │  - Returns: List<Tag> with diagnostics                              │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└────────────┬────────────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        DATABASE LAYER                                    │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      MongoDB Collections                            │ │
│  │                                                                      │ │
│  │  patients                    contactusers              ancillaryusers│ │
│  │  ┌──────────────┐            ┌──────────────┐         ┌───────────┐│ │
│  │  │ _id          │            │ _id          │         │ _id       ││ │
│  │  │ agencyInfo   │            │ firstName    │         │ name      ││ │
│  │  │ ...          │            │ lastName     │         │ email     ││ │
│  │  │ tags: [      │            │ ...          │         │ ...       ││ │
│  │  │   {          │            │ tags: [...]  │         │ tags: [...] │ │
│  │  │     namespace│            └──────────────┘         └───────────┘│ │
│  │  │     name     │                   ▲                       ▲       │ │
│  │  │     source   │                   │                       │       │ │
│  │  │     confidence│            Indexes:                Indexes:      │ │
│  │  │     value    │            - tags.namespace        - tags.namespace│ │
│  │  │     metadata │            - tags.name             - tags.name   │ │
│  │  │   }          │            - composite             - composite   │ │
│  │  │ ]            │                                                   │ │
│  │  └──────────────┘                                                   │ │
│  │        ▲                                                             │ │
│  │        │                                                             │ │
│  │  Indexes:                                                            │ │
│  │  - tags.namespace                                                    │ │
│  │  - tags.name                                                         │ │
│  │  - composite                                                         │ │
│  │                                                                      │ │
│  │  tag_catalog                                                         │ │
│  │  ┌──────────────────────────────────────────────────────────────┐  │ │
│  │  │ TagDefinition                                                 │  │ │
│  │  │ - namespace, name (unique index)                              │  │ │
│  │  │ - displayName, description                                    │  │ │
│  │  │ - category, isSensitive                                       │  │ │
│  │  │ - allowedRoles, allowedTaggers                                │  │ │
│  │  │ - isDeprecated, replacedBy                                    │  │ │
│  │  │ - icon, color, retentionDays                                  │  │ │
│  │  └──────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                         ADMIN ENDPOINTS                                  │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │  POST /api/admin/tags/catalog/seed     - Initialize catalog       │ │
│  │  GET  /api/admin/tags/catalog          - List definitions         │ │
│  │  POST /api/admin/tags/catalog          - Create definition        │ │
│  │  POST /api/admin/tags/backfill         - Backfill tags            │ │
│  │  GET  /api/admin/tags/rules            - List rules               │ │
│  │  POST /api/admin/tags/evaluate         - Evaluate entity          │ │
│  │  GET  /api/admin/tags/statistics       - Usage stats              │ │
│  │  GET  /api/admin/tags/validate-system  - Health check             │ │
│  └────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘

TAG FLOW EXAMPLE:
=================

1. Client creates Patient via POST /api/patients
2. Endpoint receives request
3. Authorization checks user roles
4. PatientRepository.CreateAsync() called
5. Repository calls TaggingService.ApplyTagsAsync()
6. TaggingService evaluates with all applicable rules:
   - PatientPhiRule: Adds PHI, clinical-data, retention:7y
   - DataQualityRule: Adds verified/suspect
   - OwnershipRule: Adds owner, creator tags
7. Tags merged and deduplicated
8. Patient saved to MongoDB with tags
9. Response returned to client with tags included

QUERY EXAMPLE:
==============

MongoDB Query: db.patients.find({ "tags.namespace": "sensitivity", "tags.name": "PHI" })
                  ↓
Uses multikey index: tags.namespace + tags.name
                  ↓
Authorization: TagAccessFilter checks user roles
                  ↓
Returns only patients user is authorized to view
```

## Tag Structure

```json
{
  "namespace": "sensitivity",
  "name": "PHI",
  "source": "rule:PatientPHI:1.0",
  "appliedAt": "2026-01-03T19:00:00Z",
  "appliedBy": "system",
  "confidence": 1.0,
  "value": null,
  "expiresAt": null,
  "metadata": {
    "reason": "Patient records inherently contain Protected Health Information",
    "regulation": "HIPAA",
    "category": "healthcare"
  },
  "version": 1
}
```

## Execution Flow Timeline

```
T0:   Client sends POST request
T1:   FastEndpoints routes to CreatePatientEndpoint
T2:   Authorization middleware validates token
T3:   PatientRepository.CreateAsync() invoked
T4:   TaggingService.ApplyTagsAsync() called
T5:   ├─ Rule 1 (PatientPhiRule) executes → 3 tags
T6:   ├─ Rule 2 (DataQualityRule) executes → 1 tag
T7:   └─ Rule 3 (OwnershipRule) executes → 1 tag
T8:   Tags deduplicated: 5 unique tags
T9:   Patient updated with tags
T10:  MongoDB insert with indexed tags
T11:  Response returned to client
Total: <10ms (excluding network I/O)
```

## Key Design Patterns

1. **Strategy Pattern**: Pluggable rules via ITaggingRule
2. **Chain of Responsibility**: Sequential rule execution
3. **Repository Pattern**: Data access abstraction
4. **Decorator Pattern**: Tag application wrapping entities
5. **Policy-Based Authorization**: ASP.NET Core policies
6. **Command Pattern**: Admin operations (backfill, seed, etc.)
7. **Factory Pattern**: Tag creation in TaggingRuleBase
8. **Immutable Records**: Tag and TagDefinition models
