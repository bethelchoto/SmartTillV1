Namespace Models
    Public Class Sale
        Public Property Id As Integer
        Public Property TotalAmount As Decimal
        Public Property CashierId As Integer
        Public Property SaleDate As DateTime = DateTime.Now
    End Class

    Public Class SaleDetail
        Public Property Id As Integer
        Public Property SaleId As Integer
        Public Property ProductId As Integer
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
    End Class
End Namespace