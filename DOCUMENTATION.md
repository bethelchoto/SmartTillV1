# SmartTill POS System — Feature Documentation

> **Version:** 1.0 | **Platform:** Windows WPF (.NET Framework 4.7.2) | **Language:** VB.NET

---

## 1. Architecture Overview

| Layer | Technology | Purpose |
|---|---|---|
| **UI Framework** | WPF (XAML) | All screens and views |
| **Pattern** | MVVM | Clean separation of UI and logic |
| **Toolkit** | CommunityToolkit.Mvvm | Observable properties, relay commands |
| **Database** | SQLite (`SmartTillV2.db`) | Local offline storage |
| **ORM** | Dapper | SQL query mapping |
| **Auth** | BCrypt.Net-Next | Secure password hashing |

**Database file location:** `bin\Debug\Data\SmartTillV2.db`

---

## 2. Authentication & Login

- **Login screen** is the application startup window
- Login is validated against the `Users` table using **BCrypt password hashing**
- A default **Admin** account (`admin` / `admin123`) is auto-seeded on first run
- **Role-based access:** Admin, Manager, Cashier
- On successful login, the user is taken to the **Main Shell / Management Console**
- The current logged-in user's name and role are displayed in the left sidebar

---

## 3. Main Shell (Management Console)

The shell wraps all views in a sidebar navigation layout:

| Nav Item | Destination |
|---|---|
| Dashboard | Home (placeholder) |
| POS Terminal | Full point-of-sale terminal |
| Products → Category Management | Manage product categories |
| Products → Product Management | View, add, delete products |
| Sales History | *(view pending)* |
| User Management | Manage system users |
| Store Settings | *(view pending)* |
| Log Out | Returns to Login window |

- **Status bar** at top-right shows the current system status (e.g., "Ready")
- Sidebar shows the logged-in user's avatar, name, and role

---

## 4. POS Terminal

The POS is the core transactional screen, split into two panels:

### 4.1 Left Panel — Cart & Product Search

#### Product Search
- Text box for barcode scan or product name search
- Searches both `Barcode` and `Name` fields simultaneously (case-insensitive)
- Shows a **live dropdown** of up to 5 matching products as you type
- Clicking a result adds it to the cart and auto-clears the search field

#### Cart (Order List)
- Displays all items: **Product Name | Unit Price | Qty | Total**
- Each row has:
  - **`+`** — increments quantity (blocked if it would exceed stock)
  - **`−`** — decrements quantity (minimum 1)
  - **🗑 Remove** — removes the line item entirely
- All totals recalculate in real time

### 4.2 Right Panel — Payment Sidebar

| Field | Description |
|---|---|
| Subtotal | Sum of all line item totals |
| Discount | Calculated from discount % input |
| VAT (15%) | Applied to subtotal after discount |
| **Total** | Final payable amount |
| Amount Paid | Cash entered by cashier |
| **Change Due** | Auto-calculated (Paid − Total) |

#### Payment Methods
Toggle buttons: **Cash | Card | EcoCash | Innbucks | Bank Transfer**

- **Cash** — cashier enters Amount Paid → Change Due auto-calculates
- **Card / EcoCash / etc.** — Amount Paid auto-set to Total, Change = $0.00

#### Action Buttons

| Button | Action |
|---|---|
| **PROCESS SALE (F2)** | Validates stock, saves sale, shows receipt |
| **HOLD SALE** | Parks the current cart for later recall |
| **ABORT SALE** | Clears the cart without saving |

---

## 5. Stock Management & Validation

Stock is protected at **three layers** to prevent negative inventory:

| Layer | When It Fires | What Happens |
|---|---|---|
| **Add to Cart** | Scanning or selecting a product | Blocked if stock = 0, or qty in cart would exceed available stock |
| **Increment (+)** | Pressing + on a cart row | Blocked if new qty > live stock |
| **Process Sale** | Clicking PROCESS SALE | Re-validates ALL items; any violation cancels the entire sale |

**Example error message:**
> *"SALE BLOCKED: 'rice' — requested 5, only 2 in stock. Please reduce the quantity."*

When a sale completes successfully, `StockQuantity` in the `Products` table is reduced for every item sold inside an **atomic database transaction** (rolled back on any failure).

---

## 6. Sale Processing Flow

```
Cashier adds items to cart
        ↓
Selects payment method
        ↓
Enters Amount Paid (Cash) or leaves blank (Card/EcoCash)
        ↓
Clicks PROCESS SALE
        ↓
[Stock Validation] ──FAIL──► Sale blocked, error shown to cashier
        ↓ PASS
[Payment Calculation] → defaults AmountPaid if blank
        ↓
[Database Transaction]
    INSERT into Sales
    INSERT into SaleDetails (one row per item)
    UPDATE Products SET StockQuantity = StockQuantity - Qty
    COMMIT
        ↓
[Success Toast] → Green banner for 3 seconds
    "Sale Successful! Total: $X.XX — Stock updated"
        ↓
[Receipt Overlay] → Full SmartTill receipt displayed
        ↓
Cashier clicks [Print Receipt]
        ↓
Cart clears, POS terminal resets
```

---

## 7. Receipt

After a successful sale the receipt overlay shows:

- **SmartTill** branded logo (purple + orange)
- Store contact details (Tel, Email)
- **Tax Invoice** header
- Customer name, Invoice number, Date
- **Itemised table:** Item | Price | Qty | Total
- Totals: Sub Total → Discount → Tax (15%) → Total Bill → Due → **Total Payable**
- "Thank You For Shopping With Us" footer
- **Print Receipt** button — closes the receipt and resets the terminal

---

## 8. Hold & Recall System

- **HOLD SALE**: Saves the entire cart (items + customer name + totals) to `HeldSales` / `HeldSaleDetails` tables with a time-stamped reference (e.g., `HOLD-1430`)
- Cart clears after a successful hold
- **Recall**: Held sales can be recalled and restored to the cart for completion

---

## 9. Product Management

- View all products: Barcode | Name | Category | Price | Stock
- **Add New Product** form: Name, Barcode, Category (dropdown), Price, Initial Stock
- **Delete** product
- **Refresh** reloads product list from the database
- Stock column is **read-only** in the grid (updated only through sales)

---

## 10. Category Management

- View all categories
- Add a new category by name
- Delete a category

---

## 11. User Management

- View all system users
- Add new users with role assignment (Admin / Manager / Cashier)
- Passwords stored as **BCrypt hashes** — never plain text

---

## 12. Database Schema

| Table | Purpose |
|---|---|
| `Users` | System user accounts |
| `Categories` | Product categories |
| `Products` | Product catalog with live stock levels |
| `Sales` | Completed sale headers |
| `SaleDetails` | Line items per sale |
| `HeldSales` | Held/paused sale headers |
| `HeldSaleDetails` | Line items for held sales |

### Auto-Migration on Startup
`InitializeDatabase()` runs on every app start and:
- Creates all tables if they don't exist
- Detects and drops stale/old-schema tables (e.g., Sales without `Subtotal` column)
- Seeds the default Admin account if none exists

---

## 13. Error Handling

| Scenario | Handling |
|---|---|
| DB save fails (ProcessSale) | Receipt closes, real SQLite error shown in status |
| Stock over-limit at checkout | Sale blocked, no DB writes, error shown |
| Unhandled UI exception | `DispatcherUnhandledException` — dialog shown, app stays open |
| Thread-level crash | `AppDomain.UnhandledException` — dialog shown |
| Empty numeric field in form | `TargetNullValue=0` prevents binding format crash |

---

## 14. Known Limitations / Planned Work

| Feature | Status |
|---|---|
| Sales History view | Navigation present — view not yet built |
| Store Settings view | Navigation present — view not yet built |
| Print to physical printer | Currently just closes the overlay |
| Dashboard charts/metrics | Placeholder only |
| Customer profiles | Walk-in Customer default only |
| Per-line-item discount | Field exists in DB — not yet exposed in POS UI |
| Receipt invoice number | Shows real Sale ID after DB save; shows 0 if DB failed |

---

*Generated: 2026-03-26 | SmartTill V1*
