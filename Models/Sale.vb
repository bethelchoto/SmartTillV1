Namespace Models
    Public Class Sale
        Public Property Id As Integer
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property AmountPaid As Decimal
        Public Property ChangeDue As Decimal
        Public Property PaymentMethod As String
        Public Property CustomerName As String
        Public Property CashierId As Integer
        Public Property SaleDate As DateTime = DateTime.Now
    End Class

    Public Class SaleDetail
        Public Property Id As Integer
        Public Property SaleId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String ' For easier display
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountPercent As Decimal = 0
        Public Property Total As Decimal
    End Class
End Namespace