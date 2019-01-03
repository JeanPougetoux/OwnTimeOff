module Client.Balance.State

open Elmish
open Fable.PowerPack
open Fable.PowerPack.Fetch
open Thoth.Json

open TimeOff
open TimeOff.AuthTypes
open Client
open Client.Balance.Types

let getUserBalance token userName =
    promise {
        let url = ServerUrls.UserBalance
        let props =
            [ Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        let! res = Fetch.fetch (url + userName) props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<UserVacationBalance> txt
    }

let init userData userToDisplay : Model * Cmd<Msg> =
  { UserData = userData; UserToDisplay = defaultArg userToDisplay userData.UserName; Balance = None }, Cmd.ofMsg FetchBalance

let update msg model =
  match msg with
  | FetchBalance ->
      model, Cmd.ofPromise (getUserBalance model.UserData.Token) model.UserToDisplay DisplayBalance NetworkError
  | DisplayBalance balance ->
      { model with Balance = Some balance }, []
  | NetworkError error ->
    printfn "[Balance.State][Network error] %s" error.Message
    model, Cmd.none
