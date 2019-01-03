[<AutoOpen>]
module Client.Global

open TimeOff.AuthTypes

type NavigationData = {
  CurrentPage: Page
  User: UserData option
}

[<RequireQualifiedAccess>]
type NotificationType =
    | Success
    | Error

type Notification = {
    NotificationType: NotificationType
    Text: string } with
    static member Success text = { NotificationType = NotificationType.Success; Text = text }
    static member Error text = { NotificationType = NotificationType.Error; Text = text }

type GlobalMsg =
    | LoggedIn of UserData
    | Logout
    | LoggedOut
    | StorageFailure of exn