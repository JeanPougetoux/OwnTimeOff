module Client.Menu.View

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fulma

open TimeOff
open Client

let menuItem label page currentPage =
    li
      [ ]
      [ a
          [ classList [ "is-active", page = currentPage ]
            Href (Pages.toPath page) ]
          [ str label ] ]

let view (model: NavigationData) =
  let currentPage = model.CurrentPage
  let isLoggedIn, isManager =
    match model.User with
    | Some userData ->
        match userData.User with
        | Manager -> true, true
        | _ -> true, false
    | _ -> false, false

  Menu.menu [ ]
    [
      Menu.label [ ] [ str "General" ]
      Menu.list [ ]
        [ yield menuItem "Home" Page.Home currentPage
          if isManager then
            yield menuItem "Employees" Page.Employees currentPage
          elif isLoggedIn then
            yield menuItem "Balance" (Page.Balance None) currentPage
          yield menuItem "About" Page.About currentPage ] ]