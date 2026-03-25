Class MainWindow
    Public Sub New()
        InitializeComponent()
        Me.DataContext = New ViewModels.MainViewModel()
    End Sub
End Class
