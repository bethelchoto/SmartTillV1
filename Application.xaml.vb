Imports System.Windows
Imports System.Windows.Threading

Class Application

    Sub New()
        ' Catch crashes on background threads (pre-dispatcher exceptions)
        AddHandler AppDomain.CurrentDomain.UnhandledException,
            Sub(s, e)
                Dim msg = If(TypeOf e.ExceptionObject Is Exception,
                             DirectCast(e.ExceptionObject, Exception).Message,
                             e.ExceptionObject.ToString())
                MessageBox.Show("Fatal error: " & msg, "SmartTill Crashed", MessageBoxButton.OK, MessageBoxImage.Error)
            End Sub
    End Sub

    Private Sub App_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs)
        MessageBox.Show("Unhandled error:" & Environment.NewLine & e.Exception.Message,
                        "SmartTill – Error", MessageBoxButton.OK, MessageBoxImage.Error)
        e.Handled = True
    End Sub

End Class
