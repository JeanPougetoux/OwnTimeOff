module Client.Navbar.View

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma
open Fable.FontAwesome

open Client

let private navButton href onclick icon txt =
    Control.div [ ]
        [ Button.a
            [ Button.Props [
                match href with
                | Some href ->
                yield Href href :> IHTMLProp
                | None -> ()
                match onclick with
                | Some onclick -> yield OnClick (fun _ -> onclick()) :> IHTMLProp
                | None -> ()
                ] ]
            [ Icon.icon [ ]
                    [ Fa.i [ Fa.Regular.FileAlt ]
                        [ ] ]
              span [] [ str txt ] ] ]

let loginStatus (model: NavigationData) dispatch =
    span
        [ ClassName "nav-item" ]
        [ div
            [ ClassName "field is-grouped" ]
            [
                if model.User = None then
                    yield navButton (Some (Pages.toPath Page.Login)) None Fa.Regular.Bell "Login"
                else
                    yield navButton None (Some (fun () -> dispatch Logout)) Fa.Regular.Angry "Logout"
            ]
        ]

let view (model: NavigationData) dispatch =
    Navbar.navbar [ ]
        [ Container.container [ ]
            [ Navbar.Start.div [ ]
                [ Navbar.Item.a [ ]
                    [ Heading.h4 [ ]
                        [ str "Time Off" ] ] ]
              Navbar.Item.div [ ]
                [ loginStatus model dispatch ] ] ]