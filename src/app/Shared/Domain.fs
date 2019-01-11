namespace TimeOff

open System

// First, we define our domain
type UserId = int

type User =
    | Employee of UserId
    | Manager

type HalfDay = | AM | PM

[<CLIMutable>]
type Boundary = {
    Date: DateTime
    HalfDay: HalfDay
}

[<CLIMutable>]
type TimeOffRequest = {
    Date: DateTime
    UserId: UserId
    RequestId: Guid
    Start: Boundary
    End: Boundary
}

[<CLIMutable>]
type UserVacationBalance = {
  UserName : UserId
  BalanceYear: int
  CarriedOver: float
  PortionAccruedToDate: float
  TakenToDate: float
  Planned: float
  CurrentBalance: float
}
