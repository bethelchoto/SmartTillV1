Imports System.IO
Imports Microsoft.Data.Sqlite
Imports Dapper
Imports System.Linq
Imports System.Collections.Generic

Namespace Services
    Public Class DatabaseService
        Private ReadOnly _dbPath As String
        Private ReadOnly _connectionString As String

        Public Sub New()
            ' Set the database path to the application data folder
            Dim appData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data")
            If Not Directory.Exists(appData) Then
                Directory.CreateDirectory(appData)
            End If

            _dbPath = Path.Combine(appData, "SmartTillV2.db")
            _connectionString = $"Data Source={_dbPath}"

            ' Initialize the database
            InitializeDatabase()
        End Sub

        Public Function GetConnection() As SqliteConnection
            Return New SqliteConnection(_connectionString)
        End Function

        Private Sub InitializeDatabase()
            Try
                Using db = GetConnection()
                    db.Open()

                    db.Execute("CREATE TABLE IF NOT EXISTS Users (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "Username TEXT NOT NULL UNIQUE," &
                               "PasswordHash TEXT NOT NULL," &
                               "Role TEXT NOT NULL," &
                               "CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP)")

                    db.Execute("CREATE TABLE IF NOT EXISTS Categories (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "Name TEXT NOT NULL UNIQUE)")

                    ' Migration: Add Permissions column if missing
                    Dim tableInfo = db.Query("PRAGMA table_info(Users)").ToList()
                    Dim hasPermissions = tableInfo.Any(Function(row) DirectCast(row.name, String).Equals("Permissions", StringComparison.OrdinalIgnoreCase))
                    If Not hasPermissions Then
                        db.Execute("ALTER TABLE Users ADD COLUMN Permissions TEXT")
                    End If

                    ' Seed default Admin account
                    Dim adminExists = db.ExecuteScalar(Of Integer)("SELECT COUNT(*) FROM Users WHERE Role = 'Admin'")
                    If adminExists = 0 Then
                        Dim hash = BCrypt.Net.BCrypt.HashPassword("admin123")
                        db.Execute("INSERT INTO Users (Username, PasswordHash, Role, Permissions) VALUES (@Username, @PasswordHash, @Role, @Permissions)",
                                   New With {.Username = "admin", .PasswordHash = hash, .Role = "Admin", .Permissions = "{""All"": true}"})
                    End If

                    db.Execute("CREATE TABLE IF NOT EXISTS Products (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "Name TEXT NOT NULL," &
                               "Barcode TEXT UNIQUE," &
                               "Price DECIMAL(10,2) NOT NULL," &
                               "StockQuantity INTEGER NOT NULL DEFAULT 0," &
                               "Category TEXT)")

                    ' --- Schema Migration for Sales tables ---
                    ' If the Sales table exists but lacks the Subtotal column, it's old schema.
                    ' Drop all sales-related tables and let them be recreated below.
                    Dim salesInfo = db.Query("PRAGMA table_info(Sales)").ToList()
                    If salesInfo.Count > 0 Then
                        Dim hasSubtotal = salesInfo.Any(Function(col) DirectCast(col.name, String).Equals("Subtotal", StringComparison.OrdinalIgnoreCase))
                        If Not hasSubtotal Then
                            db.Execute("DROP TABLE IF EXISTS HeldSaleDetails")
                            db.Execute("DROP TABLE IF EXISTS HeldSales")
                            db.Execute("DROP TABLE IF EXISTS SaleDetails")
                            db.Execute("DROP TABLE IF EXISTS Sales")
                        End If
                    End If

                    db.Execute("CREATE TABLE IF NOT EXISTS Sales (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "Subtotal DECIMAL," &
                               "DiscountAmount DECIMAL," &
                               "TaxAmount DECIMAL," &
                               "TotalAmount DECIMAL," &
                               "AmountPaid DECIMAL," &
                               "ChangeDue DECIMAL," &
                               "PaymentMethod TEXT," &
                               "CustomerName TEXT," &
                               "CashierId INTEGER," &
                               "SaleDate TEXT)")

                    db.Execute("CREATE TABLE IF NOT EXISTS SaleDetails (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "SaleId INTEGER," &
                               "ProductId INTEGER," &
                               "Quantity INTEGER," &
                               "UnitPrice DECIMAL," &
                               "DiscountPercent DECIMAL," &
                               "Total DECIMAL)")

                    db.Execute("CREATE TABLE IF NOT EXISTS HeldSales (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "CashierId INTEGER," &
                               "Subtotal DECIMAL," &
                               "DiscountAmount DECIMAL," &
                               "TaxAmount DECIMAL," &
                               "TotalAmount DECIMAL," &
                               "CustomerName TEXT," &
                               "HeldDate TEXT," &
                               "Reference TEXT)")

                    db.Execute("CREATE TABLE IF NOT EXISTS HeldSaleDetails (" &
                               "Id INTEGER PRIMARY KEY AUTOINCREMENT," &
                               "HeldSaleId INTEGER," &
                               "ProductId INTEGER," &
                               "ProductName TEXT," &
                               "Quantity INTEGER," &
                               "UnitPrice DECIMAL," &
                               "DiscountPercent DECIMAL," &
                               "Total DECIMAL)")
                End Using
            Catch ex As Exception
                System.Windows.MessageBox.Show("Database initialization failed:" & Environment.NewLine & ex.Message,
                                               "SmartTill – DB Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
            End Try
        End Sub

        Public Function AuthenticateUser(username As String, password As String) As Models.User
            Using db = GetConnection()
                db.Open()
                Dim user = db.QueryFirstOrDefault(Of Models.User)("SELECT * FROM Users WHERE Username = @Username", New With {.Username = username})

                If user IsNot Nothing AndAlso BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) Then
                    Return user
                End If
            End Using
            Return Nothing
        End Function

        Public Function CreateUser(username As String, password As String, role As String, permissions As String) As Boolean
            Try
                Dim hashedPassword = BCrypt.Net.BCrypt.HashPassword(password)
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Users (Username, PasswordHash, Role, Permissions) VALUES (@Username, @PasswordHash, @Role, @Permissions)",
                                New With {.Username = username, .PasswordHash = hashedPassword, .Role = role, .Permissions = permissions})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function GetAllUsers() As List(Of Models.User)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.User)("SELECT * FROM Users").ToList()
            End Using
        End Function

        Public Function DeleteUser(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Users WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function UpdateUser(user As Models.User) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("UPDATE Users SET Role = @Role, Permissions = @Permissions WHERE Id = @Id",
                                New With {.Role = user.Role, .Permissions = user.Permissions, .Id = user.Id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function VerifyDatabase() As String
            Try
                If Not File.Exists(_dbPath) Then
                    Return "Database file not found."
                End If

                Using db = GetConnection()
                    db.Open()
                    Dim count = db.ExecuteScalar(Of Integer)("SELECT COUNT(*) FROM Users")
                    Return $"Database verified successfully. Tables exist and are accessible. Location: {_dbPath}"
                End Using
            Catch ex As Exception
                Return $"Database verification failed: {ex.Message}"
            End Try
        End Function
        Public Function GetAllProducts() As List(Of Models.Product)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.Product)("SELECT * FROM Products").ToList()
            End Using
        End Function

        Public Function CreateProduct(name As String, barcode As String, price As Decimal, stock As Integer, category As String) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Products (Name, Barcode, Price, StockQuantity, Category) VALUES (@Name, @Barcode, @Price, @Stock, @Category)",
                                New With {.Name = name, .Barcode = barcode, .Price = price, .Stock = stock, .Category = category})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function DeleteProduct(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Products WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function UpdateProduct(product As Models.Product) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("UPDATE Products SET Name = @Name, Barcode = @Barcode, Price = @Price, StockQuantity = @StockQuantity, Category = @Category WHERE Id = @Id",
                                New With {.Name = product.Name, .Barcode = product.Barcode, .Price = product.Price, .StockQuantity = product.StockQuantity, .Category = product.Category, .Id = product.Id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        ' Category CRUD
        Public Function GetAllCategories() As List(Of Models.Category)
            Using db = GetConnection()
                db.Open()
                Return db.Query(Of Models.Category)("SELECT * FROM Categories").ToList()
            End Using
        End Function

        Public Function CreateCategory(name As String) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("INSERT INTO Categories (Name) VALUES (@Name)", New With {.Name = name})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function DeleteCategory(id As Integer) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    db.Execute("DELETE FROM Categories WHERE Id = @Id", New With {.Id = id})
                    Return True
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function
        ' Sale Processing — returns (SaleId, ErrorMessage). SaleId=0 on failure.
        Public Function ProcessSale(sale As Models.Sale, details As List(Of Models.SaleDetail)) As (Id As Integer, ErrorMsg As String)
            Using db = GetConnection()
                db.Open()
                Using transaction = db.BeginTransaction()
                    Try
                        ' Step 1: Insert the Sale header
                        Dim sqlSale = "INSERT INTO Sales (Subtotal, DiscountAmount, TaxAmount, TotalAmount, AmountPaid, ChangeDue, PaymentMethod, CustomerName, CashierId, SaleDate) " &
                                      "VALUES (@Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, @AmountPaid, @ChangeDue, @PaymentMethod, @CustomerName, @CashierId, @SaleDate)"
                        db.Execute(sqlSale, sale, transaction)

                        ' Step 2: Retrieve the assigned rowid (separate statement required by Microsoft.Data.Sqlite)
                        Dim saleId As Integer = db.QueryFirstOrDefault(Of Integer)("SELECT last_insert_rowid()", Nothing, transaction)

                        ' Step 3: Insert each line item and deduct stock
                        For Each detail In details
                            db.Execute(
                                "INSERT INTO SaleDetails (SaleId, ProductId, Quantity, UnitPrice, DiscountPercent, Total) " &
                                "VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice, @DiscountPercent, @Total)",
                                New With {.SaleId = saleId, .ProductId = detail.ProductId,
                                          .Quantity = detail.Quantity, .UnitPrice = detail.UnitPrice,
                                          .DiscountPercent = detail.DiscountPercent, .Total = detail.Total},
                                transaction)

                            db.Execute(
                                "UPDATE Products SET StockQuantity = StockQuantity - @Qty WHERE Id = @Id",
                                New With {.Qty = detail.Quantity, .Id = detail.ProductId},
                                transaction)
                        Next

                        transaction.Commit()
                        Return (saleId, String.Empty)

                    Catch ex As Exception
                        transaction.Rollback()
                        ' Show the real error so it can be diagnosed
                        System.Windows.MessageBox.Show("Sale DB Error: " & ex.Message,
                                                       "ProcessSale Failed", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error)
                        Return (0, ex.Message)
                    End Try
                End Using
            End Using
        End Function

        Public Function GetProductByBarcode(barcode As String) As Models.Product
            Using db = GetConnection()
                db.Open()
                Return db.QueryFirstOrDefault(Of Models.Product)("SELECT * FROM Products WHERE Barcode = @Barcode", New With {.Barcode = barcode})
            End Using
        End Function

        ' Held Sales Methods
        Public Function HoldSale(sale As Models.Sale, details As List(Of Models.SaleDetail), reference As String) As Boolean
            Try
                Using db = GetConnection()
                    db.Open()
                    Using trans = db.BeginTransaction()
                        ' Step 1: Insert HeldSale
                        Dim sqlSale = "INSERT INTO HeldSales (CashierId, Subtotal, DiscountAmount, TaxAmount, TotalAmount, CustomerName, HeldDate, Reference) " &
                                     "VALUES (@CashierId, @Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, @CustomerName, @SaleDate, @Reference)"
                        db.Execute(sqlSale, New With {
                            sale.CashierId, sale.Subtotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount, sale.CustomerName, .SaleDate = DateTime.Now, reference
                        }, trans)

                        ' Step 2: Get ID separately
                        Dim heldId = db.ExecuteScalar(Of Integer)("SELECT last_insert_rowid()", Nothing, trans)

                        For Each item In details
                            Dim sqlItem = "INSERT INTO HeldSaleDetails (HeldSaleId, ProductId, ProductName, Quantity, UnitPrice, DiscountPercent, Total) " &
                                         "VALUES (@HeldSaleId, @ProductId, @ProductName, @Quantity, @UnitPrice, @DiscountPercent, @Total)"
                            db.Execute(sqlItem, New With {
                                .HeldSaleId = heldId, item.ProductId, item.ProductName, item.Quantity, item.UnitPrice, item.DiscountPercent, item.Total
                            }, trans)
                        Next

                        trans.Commit()
                        Return True
                    End Using
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Function GetHeldSales() As IEnumerable(Of Object)
            Using db = GetConnection()
                db.Open()
                Return db.Query("SELECT * FROM HeldSales ORDER BY HeldDate DESC")
            End Using
        End Function

        Public Function RecallHeldSale(heldId As Integer) As (Sale As Models.Sale, Details As List(Of Models.SaleDetail))
            Try
                Using db = GetConnection()
                    db.Open()
                    Dim sale = db.QueryFirstOrDefault(Of Models.Sale)("SELECT * FROM HeldSales WHERE Id = @Id", New With {.Id = heldId})
                    Dim details = db.Query(Of Models.SaleDetail)("SELECT * FROM HeldSaleDetails WHERE HeldSaleId = @Id", New With {.Id = heldId}).ToList()

                    ' Remove from held queue after recall
                    db.Execute("DELETE FROM HeldSaleDetails WHERE HeldSaleId = @Id", New With {.Id = heldId})
                    db.Execute("DELETE FROM HeldSales WHERE Id = @Id", New With {.Id = heldId})

                    Return (sale, details)
                End Using
            Catch ex As Exception
                Return (Nothing, New List(Of Models.SaleDetail)())
            End Try
        End Function
    End Class
End Namespace