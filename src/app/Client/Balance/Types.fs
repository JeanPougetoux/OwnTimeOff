module Client.Balance.Types

open TimeOff
open TimeOff.AuthTypes

type Model = {
  UserData: UserData
  UserToDisplay: string
  Balance: UserVacationBalance option
}

type Msg =
  | FetchBalance
  | DisplayBalance of UserVacationBalance
  | NetworkError of exn
