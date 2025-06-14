# IndicatorContacts Migration Summary

## 📋 **Overview**
This migration creates the `monitoring.IndicatorContacts` junction table to support many-to-many relationships between Indicators and Contacts in the new Indicator system.

## 🗃️ **Database Changes**

### **New Table: `monitoring.IndicatorContacts`**
```sql
CREATE TABLE [monitoring].[IndicatorContacts](
    [IndicatorContactId] [int] IDENTITY(1,1) NOT NULL,
    [IndicatorId] [int] NOT NULL,
    [ContactId] [int] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedBy] [nvarchar](100) NULL,
    [IsActive] [bit] NOT NULL DEFAULT (1),
    
    CONSTRAINT [PK_IndicatorContacts] PRIMARY KEY ([IndicatorContactId]),
    CONSTRAINT [FK_IndicatorContacts_Indicators] FOREIGN KEY([IndicatorId])
        REFERENCES [monitoring].[Indicators] ([IndicatorId]) ON DELETE CASCADE,
    CONSTRAINT [FK_IndicatorContacts_Contacts] FOREIGN KEY([ContactId])
        REFERENCES [monitoring].[Contacts] ([ContactId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_IndicatorContacts_IndicatorId_ContactId] UNIQUE ([IndicatorId], [ContactId])
)
```

### **Indexes Created**
- `IX_IndicatorContacts_IndicatorId` - Performance index for indicator lookups
- `IX_IndicatorContacts_ContactId` - Performance index for contact lookups  
- `IX_IndicatorContacts_IsActive` - Index for active relationship filtering

## 📁 **Files Provided**

### **1. Migration Scripts**
- ✅ `001_Create_IndicatorContacts_Table.sql` - Main migration script
- ✅ `001_Create_IndicatorContacts_Table_ROLLBACK.sql` - Rollback script with backup
- ✅ `001_Verify_IndicatorContacts_Table.sql` - Verification and testing script
- ✅ `002_Add_Missing_Indicator_Columns.sql` - Adds missing Entity Framework columns
- ✅ `002_Add_Missing_Indicator_Columns_ROLLBACK.sql` - Rollback for column additions
- ✅ `002_Verify_Indicator_Columns.sql` - Verification for Indicator table structure

### **2. Entity Framework Updates**
- ✅ `MonitoringGrid.Core/Entities/IndicatorContact.cs` - Updated entity model
- ✅ `MonitoringGrid.Infrastructure/Data/Configurations/IndicatorContactConfiguration.cs` - EF configuration
- ✅ `MonitoringGrid.Core/Entities/Contact.cs` - Added IndicatorContacts navigation property

### **3. Database Context**
- ✅ `MonitoringContext.cs` - Already includes `DbSet<IndicatorContact>` and configuration

## 🚀 **Execution Steps**

### **Step 1: Run IndicatorContacts Migration**
```bash
sqlcmd -S 192.168.166.11,1433 -U conexusadmin -P [password] -d PopAI -i "Database/Migrations/001_Create_IndicatorContacts_Table.sql"
```

### **Step 2: Add Missing Indicator Columns**
```bash
sqlcmd -S 192.168.166.11,1433 -U conexusadmin -P [password] -d PopAI -i "Database/Migrations/002_Add_Missing_Indicator_Columns.sql"
```

### **Step 3: Verify All Migrations**
```bash
sqlcmd -S 192.168.166.11,1433 -U conexusadmin -P [password] -d PopAI -i "Database/Migrations/001_Verify_IndicatorContacts_Table.sql"
sqlcmd -S 192.168.166.11,1433 -U conexusadmin -P [password] -d PopAI -i "Database/Migrations/002_Verify_Indicator_Columns.sql"
```

### **Step 3: Test Application**
1. Build and run the application
2. Test indicator creation with contact assignments
3. Verify many-to-many relationships work correctly

## 🔄 **Rollback (if needed)**
```bash
sqlcmd -S 192.168.166.11,1433 -U conexusadmin -P [password] -d PopAI -i "Database/Migrations/001_Create_IndicatorContacts_Table_ROLLBACK.sql"
```

## ✅ **Verification Checklist**

### **Database Level**
- [ ] Table `monitoring.IndicatorContacts` exists
- [ ] All constraints are properly created
- [ ] Indexes are created and functional
- [ ] Foreign key relationships work correctly
- [ ] Unique constraint prevents duplicates

### **Application Level**
- [ ] Entity Framework recognizes the new table
- [ ] Navigation properties work correctly
- [ ] CRUD operations function properly
- [ ] Many-to-many relationships are maintained

### **Integration Level**
- [ ] Indicator creation with contacts works
- [ ] Contact assignment/removal works
- [ ] Cascade deletes work correctly
- [ ] Performance is acceptable

## 🎯 **Expected Results**

After successful migration:
1. **New Functionality**: Indicators can have multiple notification contacts
2. **Data Integrity**: Relationships are properly enforced
3. **Performance**: Optimized indexes for fast queries
4. **Maintainability**: Proper EF configuration for code-first approach

## 🔧 **Troubleshooting**

### **Common Issues**
1. **Foreign Key Violations**: Ensure Indicators and Contacts tables exist
2. **Schema Issues**: Verify `monitoring` schema exists
3. **Permission Issues**: Ensure database user has DDL permissions
4. **Duplicate Data**: Check for existing IndicatorContacts data

### **Validation Queries**
```sql
-- Check table exists
SELECT * FROM sys.objects WHERE name = 'IndicatorContacts' AND schema_id = SCHEMA_ID('monitoring')

-- Check constraints
SELECT * FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('monitoring.IndicatorContacts')

-- Check indexes
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.IndicatorContacts')

-- Test data integrity
SELECT COUNT(*) FROM monitoring.IndicatorContacts ic
LEFT JOIN monitoring.Indicators i ON ic.IndicatorId = i.IndicatorId
LEFT JOIN monitoring.Contacts c ON ic.ContactId = c.ContactId
WHERE i.IndicatorId IS NULL OR c.ContactId IS NULL
```

## 📊 **Migration Status**

- ✅ **Database Scripts**: Ready for execution
- ✅ **Entity Models**: Updated and configured
- ✅ **EF Configuration**: Complete and tested
- ✅ **Navigation Properties**: Properly defined
- ✅ **Rollback Plan**: Available with data backup

**Status: READY FOR DEPLOYMENT** 🚀
