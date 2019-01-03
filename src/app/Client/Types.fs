module Client.Types

/// The composed set of messages that update the state of the application
type AppMsg =
    | GlobalMsg of GlobalMsg
    | LoginMsg of Login.Types.Msg
    | HomeMsg of Home.Types.Msg
    | BalanceMsg of Balance.Types.Msg

/// The composed model for the different possible page states of the application that are lost when we change the current page
type TransientPageModel =
    | NoPageModel
    | LoginModel of Login.Types.Model
    | BalanceModel of Balance.Types.Model

/// The composed model for the application, which is a single page state plus login information
type Model = {
        Navigation: NavigationData
        TransientPageModel: TransientPageModel
        Home: Home.Types.Model // The home page model is stored separately in order to keep the state even when we navigate on other pages
    }