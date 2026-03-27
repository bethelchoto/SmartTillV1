# SmartTill — Multi-Terminal WITH SQLite
*How to share a single SQLite database across multiple PCs safely.*

## The Core Problem
SQLite is **file-based**. If multiple PCs try to open the same file over a network share (like `\\SERVER\Data\db.db`), you will get:
- `Database is locked` errors.
- Random crashes.
- Potential data corruption.

## The Solution: The "Hub and Spoke" Rules

### Rule 1: The Middleman (API)
Only **ONE** computer (the Server PC) touches the `SmartTillV2.db` file. All other computers (Terminals) talk to the Server PC over the network using a tiny Web API.

### Rule 2: Terminal Access
Terminals do **not** have a direct database connection string. Instead, they have a `ServerAddress` (e.g., `192.168.1.100`) and use an `ApiClient` to request data.

### Rule 3: Database Optimization (WAL Mode)
The database must be set to **Write-Ahead Logging (WAL)** mode. This allows the API to read data while a sale is being written, preventing lag.

---

## Architecture Diagram

```
[ TERMINAL 2 ] ---- (HTTP) ----┐
[ TERMINAL 3 ] ---- (HTTP) ----┤--> [ SERVER PC ] --> [ SQLite DB ]
[ TERMINAL 1 ] ---- (Local) ---┘      (Runs API)
```

## Implementation Steps
1. **Enable WAL Mode**: Add `PRAGMA journal_mode=WAL;` to your setup code.
2. **Create API**: Build a small project that "wraps" your `DatabaseService`.
3. **Switch Terminals**: Use an `ApiClient` in the WPF app if the mode is set to "Terminal".
