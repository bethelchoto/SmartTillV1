# SmartTill Scalability Roadmap
*Tailored to your current VB.NET WPF + MVVM + Dapper + SQLite stack*

---

## 📊 Scalability Tiers

### Tier 1 — Refactor for Solid Foundation (Do This NOW)
**Goal**: Split `DatabaseService.vb` into focused services (Repositories).
- **Problem**: Current `DatabaseService` is a "God Object" handling everything.
- **Fix**: Use Repository Pattern (`UserRepository`, `ProductRepository`, `SaleRepository`).

### Tier 2 — True Multi-User on One Machine
**Goal**: Multiple cashiers logging in on the same PC.
- **Missing**: `SessionContext` service and an `AuditLog` table for accountability.

### Tier 3 — Multi-Terminal on a LAN (RetailMan-level)
**Goal**: 2–5 POS terminals in one shop sharing live data.
- **Option A**: Migrate to **SQL Server Express** (Free). Best for many concurrent users.
- **Option B**: **SQLite + Local API**. Keep SQLite, but only one PC touches it via a middleman API.

### Tier 4 — Multi-Store with Central Server
**Goal**: Head office can see all store sales in real time.
- Requires `StoreId` columns and a sync/replication layer.

---

## 🔑 Key Principle
Because you chose **Dapper** instead of Entity Framework, your SQL queries are safe and portable. Moving from SQLite to SQL Server is ~90% code-compatible if you ever decide to switch.
