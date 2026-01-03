# Tagging System - Quick Start Guide

## 5-Minute Setup

### Step 1: Start the Application

```bash
cd backend-api
dotnet run
```

The API will be available at `http://localhost:5000` (or configured port).

### Step 2: Initialize Tag Catalog

```bash
curl -X POST http://localhost:5000/api/admin/tags/catalog/seed \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

**Response:**
```json
{
  "totalDefinitions": 13,
  "created": 13,
  "skipped": 0,
  "errors": []
}
```

This creates standard tag definitions for PHI, PII, business types, lifecycle stages, quality indicators, and retention policies.

### Step 3: Validate System Health

```bash
curl http://localhost:5000/api/admin/tags/validate-system \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Expected Response:**
```json
{
  "timestamp": "2026-01-03T19:00:00Z",
  "validatedBy": "admin@example.com",
  "isHealthy": true,
  "ruleCount": 8,
  "enabledRuleCount": 8,
  "catalogTagCount": 13,
  "checks": [
    "✓ Found 8 tagging rules (8 enabled)",
    "✓ Tag catalog contains 13 definitions",
    "✓ Sample patient generated 5 tags from 4 rules",
    "✓ PHI tag correctly applied to patient",
    "✅ Tagging system is healthy and operational"
  ],
  "warnings": [],
  "errors": []
}
```

### Step 4: Preview Tag Backfill (Dry Run)

Test tagging on existing data without making changes:

```bash
curl -X POST http://localhost:5000/api/admin/tags/backfill \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "collectionName": "patients",
    "dryRun": true,
    "batchSize": 100
  }'
```

**Response:**
```json
{
  "collectionName": "patients",
  "totalEntities": 150,
  "processedEntities": 150,
  "successfullyTagged": 150,
  "totalTagsAdded": 750,
  "tagsByNamespace": {
    "sensitivity": 300,
    "business": 225,
    "quality": 150,
    "retention": 150
  },
  "isDryRun": true,
  "duration": "00:00:05.123"
}
```

### Step 5: Apply Tags to Production Data

Once you're satisfied with the dry run results:

```bash
curl -X POST http://localhost:5000/api/admin/tags/backfill \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "collectionName": "patients",
    "dryRun": false,
    "batchSize": 100
  }'
```

## How It Works

### Automatic Tagging

Once configured, tags are **automatically applied** on:
- **Create operations**: New entities get tagged immediately
- **Update operations**: Tags are refreshed on modifications
- **Backfill operations**: Apply tags to existing data

### Example: Creating a Patient

```bash
POST /api/patients
{
  "agencyInfo": {
    "patientFName": "John",
    "patientLName": "Doe",
    "dob": "01/15/1980",
    "email": "john.doe@example.com"
  }
}
```

**Result:** Patient is automatically tagged with:
- `sensitivity:PHI` (Protected Health Information)
- `sensitivity:PII` (Personally Identifiable Information)
- `retention:7y` (7-year HIPAA retention)
- `quality:verified` (No data quality issues)

### Example: Query Tagged Data

MongoDB queries work naturally with tags:

```javascript
// Find all patients with PHI
db.patients.find({ "tags.namespace": "sensitivity", "tags.name": "PHI" })

// Find high-quality patient records
db.patients.find({ "tags.namespace": "quality", "tags.name": "verified" })

// Find home health agencies
db.ancillaryusers.find({ "tags.namespace": "business", "tags.name": "home-health" })
```

## Common Use Cases

### 1. Compliance & Governance

**Find all PHI records:**
```javascript
db.patients.find({ 
  "tags": { 
    $elemMatch: { "namespace": "sensitivity", "name": "PHI" } 
  } 
})
```

**Audit retention requirements:**
```javascript
db.patients.find({ 
  "tags": { 
    $elemMatch: { "namespace": "retention", "name": "7y" } 
  } 
})
```

### 2. Data Quality Management

**Find suspect data quality:**
```javascript
db.contactusers.find({ 
  "tags": { 
    $elemMatch: { "namespace": "quality", "name": "suspect" } 
  } 
})
```

**Find placeholder emails:**
```javascript
db.contactusers.find({ 
  "tags": { 
    $elemMatch: { 
      "namespace": "quality", 
      "name": "placeholder-email" 
    } 
  } 
})
```

### 3. Business Intelligence

**Find home health agencies:**
```javascript
db.ancillaryusers.find({ 
  "tags": { 
    $elemMatch: { "namespace": "business", "name": "home-health" } 
  } 
})
```

**Find premium tier customers:**
```javascript
db.contactusers.find({ 
  "tags": { 
    $elemMatch: { "namespace": "lifecycle", "name": "premium" } 
  } 
})
```

### 4. Access Control & Routing

**Find records owned by specific user:**
```javascript
db.contactusers.find({ 
  "tags": { 
    $elemMatch: { 
      "namespace": "access", 
      "name": "owner",
      "value": "john.smith@doctoralliance.com"
    } 
  } 
})
```

## API Reference (Quick)

| Endpoint | Purpose | Example |
|----------|---------|---------|
| `POST /api/admin/tags/catalog/seed` | Initialize catalog | See Step 2 |
| `GET /api/admin/tags/validate-system` | Health check | See Step 3 |
| `POST /api/admin/tags/backfill` | Tag existing data | See Step 4 |
| `GET /api/admin/tags/rules` | List rules | `curl .../rules` |
| `GET /api/admin/tags/catalog` | List tag definitions | `curl .../catalog` |
| `POST /api/admin/tags/evaluate` | Preview tags for entity | See below |

### Preview Tags for Single Entity

```bash
curl -X POST http://localhost:5000/api/admin/tags/evaluate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "entity": {
      "agencyInfo": {
        "patientFName": "Jane",
        "patientLName": "Smith",
        "dob": "03/20/1975"
      }
    }
  }'
```

**Response:**
```json
{
  "success": true,
  "tags": [
    {
      "namespace": "sensitivity",
      "name": "PHI",
      "source": "rule:PatientPHI:1.0",
      "confidence": 1.0
    },
    {
      "namespace": "sensitivity",
      "name": "PII",
      "source": "rule:PatientPHI:1.0",
      "confidence": 1.0
    }
  ],
  "executedRules": ["PatientPHI", "DataQuality"],
  "diagnostics": [
    "Tagged as PHI: Patient record",
    "Found: First Name",
    "Found: Last Name"
  ]
}
```

## Authorization

### Required Roles

| Operation | Required Role |
|-----------|---------------|
| View PHI data | Admin, Clinician, PHI-Reader |
| View PII data | Admin, User, PII-Reader |
| Admin operations | Admin |
| View tag catalog | Any authenticated user |

### Testing Authorization (Dev Mode)

If `DEV_AUTH__ENABLED=true` in configuration, a synthetic user is injected:

```json
{
  "name": "dev-user@example.com",
  "roles": ["Admin", "Clinician", "PHI-Reader"]
}
```

## Monitoring

### Check Logs

```bash
# View tagging activity
tail -f logs/app.log | grep "Tagged"

# View rule execution
tail -f logs/app.log | grep "Rule"

# View access denials
tail -f logs/app.log | grep "Access denied"
```

### Get Statistics

```bash
curl http://localhost:5000/api/admin/tags/statistics \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Troubleshooting

### Problem: No tags applied

**Check rule status:**
```bash
curl http://localhost:5000/api/admin/tags/rules \
  -H "Authorization: Bearer YOUR_TOKEN"
```

All rules should show `isEnabled: true`.

### Problem: Authorization failures

**Verify your token claims:**
```bash
curl http://localhost:5000/health \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Check that `authentication.claims` includes required roles.

### Problem: Catalog empty

**Re-seed catalog:**
```bash
curl -X POST http://localhost:5000/api/admin/tags/catalog/seed \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Performance Tips

1. **Batch Size**: Use 100-500 for backfills depending on entity complexity
2. **Parallel Processing**: Run backfills for different collections in parallel
3. **Off-Peak Hours**: Schedule large backfills during low-traffic periods
4. **Indexes**: Ensure tag indexes are created (automatic on first run)

## Next Steps

- Review [TAGGING_SYSTEM.md](TAGGING_SYSTEM.md) for detailed documentation
- Add custom rules for your specific business logic
- Configure role-based access control for your organization
- Set up monitoring and alerting for tag-based compliance

---

**Need Help?**
- Check validation: `GET /api/admin/tags/validate-system`
- Review logs for detailed diagnostics
- All operations are logged with timestamps and user attribution
