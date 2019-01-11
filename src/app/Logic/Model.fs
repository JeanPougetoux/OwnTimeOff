namespace TimeOff

open System

// Then our commands
type Command =
    | RequestTimeOff of TimeOffRequest
    | CancelRequest of UserId * Guid
    | RefuseCancelationRequest of UserId * Guid
    | AcceptCancelationRequest of UserId * Guid
    | ValidateRequest of UserId * Guid 
    | RefuseRequest of UserId * Guid with
    member this.UserId =
        match this with
        | RequestTimeOff request -> request.UserId
        | CancelRequest (userId, _) -> userId
        | RefuseCancelationRequest (userId, _) -> userId
        | AcceptCancelationRequest (userId, _) -> userId
        | ValidateRequest (userId, _) -> userId
        | RefuseRequest (userId, _) -> userId


// And our events
type RequestEvent =
    | RequestCreated of TimeOffRequest
    | RequestCancelled of TimeOffRequest
    | RequestAskedToCancel of TimeOffRequest
    | RequestValidated of TimeOffRequest
    | RequestRefused of TimeOffRequest with
    member this.Request =
        match this with
        | RequestCreated request -> request
        | RequestCancelled request -> request
        | RequestAskedToCancel request -> request
        | RequestValidated request -> request
        | RequestRefused request -> request

// We then define the state of the system,
// and our 2 main functions `decide` and `evolve`
module Logic =

    type RequestState =
        | NotCreated
        | Cancelled of TimeOffRequest
        | PendingCancelling of TimeOffRequest
        | PendingValidation of TimeOffRequest
        | Validated of TimeOffRequest
        | Refused of TimeOffRequest with
        member this.Request =
            match this with
            | NotCreated -> invalidOp "Not created"
            | Cancelled request
            | PendingCancelling request -> request
            | PendingValidation request -> request
            | Validated request -> request
            | Refused request -> request
        member this.IsActive =
            match this with
            | NotCreated -> false
            | Cancelled _ -> false
            | PendingCancelling _ -> true
            | PendingValidation _ -> true
            | Validated _ -> true
            | Refused _ -> false

    type UserRequestsState = Map<Guid, RequestState>

    // REQUEST METHODS

    let evolveRequest state event =
        match event with
        | RequestCreated request -> PendingValidation request
        | RequestCancelled request -> Cancelled request
        | RequestAskedToCancel request -> PendingCancelling request
        | RequestValidated request -> Validated request
        | RequestRefused request -> Refused request

    let evolveUserRequests (userRequests: UserRequestsState) (event: RequestEvent) =
        let requestState = defaultArg (Map.tryFind event.Request.RequestId userRequests) NotCreated
        let newRequestState = evolveRequest requestState event
        userRequests.Add (event.Request.RequestId, newRequestState)

    let requestIn request1 request2 =
        (request1.Start >= request2.Start && request1.Start <= request2.End) 
        || (request1.End >= request2.Start && request1.End <= request2.End)

    let overlapsWith request1 request2 =
        request1 |> requestIn request2
        || request2 |> requestIn request1

    let overlapsWithAnyRequest (otherRequests: TimeOffRequest seq) request =
        otherRequests |> Seq.exists (overlapsWith request)

    let isIn today request =
        today >= request.Start.Date && today <= request.End.Date

    let createRequest today activeUserRequests  request =
        if request |> overlapsWithAnyRequest activeUserRequests then
            Error "Overlapping request"
        elif request.Start.Date <= today then
            Error "The request starts in the past"
        elif request.Start.Date <= request.Date then
            Error "The request starts in the past"
        else
            Ok [RequestCreated request]

    let validateRequest requestState =
        match requestState with
        | PendingValidation request ->
            Ok [RequestValidated request]
        | PendingCancelling request ->
            Ok [RequestValidated request]
        | _ ->
            Error "Request cannot be validated"

    let refuseRequest requestState =
        match requestState with
        | PendingValidation request ->
            Ok [RequestRefused request]
        | _ ->
            Error "Request cannot be refused"
      
    let cancelRequestByUser request (today : DateTime) =
        if today >= request.Start.Date then
            Ok [RequestAskedToCancel request]
        else
            Ok [RequestCancelled request]
    
    // DAYS DIFFERENCES CALCULATION

    let difference (date1 : DateTime) (date2 : DateTime) =
        let span = DateTime(date2.Year, date2.Month, date2.Day, 23, 59, 59) - DateTime(date1.Year, date1.Month, date1.Day, 0, 0, 0)
        span.Add(TimeSpan(0, 0, 1)).TotalDays

    // ACCOUNT METHODS

    let daysOffAdding (today : DateTime) = 
        (float (today.Month - 1)) * 2.5

    let reportDaysOffAdding =
        20.0
    
    let calculateRequestInDays request =
        let start = request.Start.Date
        let endof = request.End.Date
        let startSub = if request.Start.HalfDay = PM then 0.5 else 0.0
        let endSub = if request.End.HalfDay = AM then 0.5 else 0.0
        let days = difference start endof
        
        days - startSub - endSub


    let takenDaysOff (today : DateTime) (userRequests : UserRequestsState) =
        userRequests
        |> Map.toSeq
        |> Seq.map (fun (_, state) -> state)
        |> Seq.where (fun state -> state.IsActive)
        |> Seq.where (fun state -> state.Request.Start.Date <= today.Date)
        |> Seq.where (fun state -> state.Request.Start.Date.Year = today.Year)
        |> Seq.map (fun state -> calculateRequestInDays state.Request)
        |> Seq.sum

    let toComeDaysOff (today : DateTime) (userRequests : UserRequestsState) =
        userRequests
        |> Map.toSeq
        |> Seq.map (fun (_, state) -> state)
        |> Seq.where (fun state -> state.IsActive)
        |> Seq.where (fun state -> state.Request.Start.Date > today.Date)
        |> Seq.where (fun state -> state.Request.Start.Date.Year = today.Year)
        |> Seq.map (fun state -> calculateRequestInDays state.Request)
        |> Seq.sum

    let daysOffSold (today : DateTime) (userRequests : UserRequestsState) = 
        (daysOffAdding today) + reportDaysOffAdding - (takenDaysOff today userRequests) - (toComeDaysOff today userRequests)
         

    let decide (today: DateTime) (userRequests: UserRequestsState) (user: User) (command: Command) =
        let relatedUserId = command.UserId
        match user with
        | Employee userId when userId <> relatedUserId ->
            Error "Unauthorized"
        | _ ->
            match command with
            | RequestTimeOff request ->
                let solde = daysOffSold today userRequests
                let requestSameGuid =
                    userRequests
                    |> Map.toSeq
                    |> Seq.map (fun (_, state) -> state.Request)
                    |> Seq.where (fun req -> req.RequestId = request.RequestId)
                    |> Seq.length

                let activeUserRequests =
                    userRequests
                    |> Map.toSeq
                    |> Seq.map (fun (_, state) -> state)
                    |> Seq.where (fun state -> state.IsActive)
                    |> Seq.map (fun state -> state.Request)

                if requestSameGuid > 0 then
                    Error "A request has already the same guid"
                elif request.End.Date < request.Start.Date then
                    Error "The request end is before the start"
                elif solde < calculateRequestInDays request then
                    Error "You have not enough days to proceed the request"
                else
                    let newRequest : TimeOffRequest = {
                        Date = today
                        UserId = request.UserId
                        RequestId = request.RequestId
                        Start = request.Start
                        End = request.End
                    }
                    createRequest today activeUserRequests newRequest

            | CancelRequest (_, requestId) ->
                let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                if not requestState.IsActive then
                    Error "The request is not active"
                elif user = Manager then
                    Ok [RequestCancelled requestState.Request]
                elif relatedUserId <> requestState.Request.UserId then
                    Error "The request should only be cancelled by the owner or a manager"
                else
                    cancelRequestByUser requestState.Request today


            | RefuseCancelationRequest (_, requestId) ->
                if user <> Manager then
                    Error "Unauthorized"
                else
                    let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                    match requestState with
                      | PendingCancelling _ -> validateRequest requestState
                      | _ -> Error "The request should be pending cancelling"

            | AcceptCancelationRequest (_, requestId) ->
                if user <> Manager then
                    Error "Unauthorized"
                else
                    let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                    match requestState with
                      | PendingCancelling request -> Ok [RequestCancelled request]
                      | _ -> Error "The request should be pending cancelling"

            | ValidateRequest (_, requestId) ->
                if user <> Manager then
                    Error "Unauthorized"
                else
                    let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                    validateRequest requestState

            | RefuseRequest (_, requestId) ->
                if user <> Manager then
                    Error "Unauthorized"
                else
                    let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                    refuseRequest requestState
