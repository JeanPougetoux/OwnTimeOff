namespace TimeOff

open System

// Then our commands
type Command =
    | RequestTimeOff of TimeOffRequest
    | CancelRequest of TimeOffRequest
    | RefuseCancelationRequest of UserId * Guid
    | ValidateRequest of UserId * Guid 
    | RefuseRequest of UserId * Guid with
    member this.UserId =
        match this with
        | RequestTimeOff request -> request.UserId
        | CancelRequest request -> request.UserId
        | RefuseCancelationRequest (userId, _) -> userId
        | ValidateRequest (userId, _) -> userId
        | RefuseRequest (userId, _) -> userId


// And our events
type RequestEvent =
    | RequestCreated of TimeOffRequest
    | RequestCancelled of TimeOffRequest
    | RequestAskedToCancel of TimeOffRequest
    | RequestValidated of TimeOffRequest with
    member this.Request =
        match this with
        | RequestCreated request -> request
        | RequestCancelled request -> request
        | RequestAskedToCancel request -> request
        | RequestValidated request -> request

// We then define the state of the system,
// and our 2 main functions `decide` and `evolve`
module Logic =

    type RequestState =
        | NotCreated
        | Cancelled of TimeOffRequest
        | PendingCancelling of TimeOffRequest
        | PendingValidation of TimeOffRequest
        | Validated of TimeOffRequest with
        member this.Request =
            match this with
            | NotCreated -> invalidOp "Not created"
            | Cancelled request -> request
            | PendingCancelling request -> request
            | PendingValidation request
            | Validated request -> request
        member this.IsActive =
            match this with
            | NotCreated -> false
            | Cancelled _ -> false
            | PendingCancelling _ -> true
            | PendingValidation _ -> true
            | Validated _ -> true

    type UserRequestsState = Map<Guid, RequestState>

    let evolveRequest state event =
        match event with
        | RequestCreated request -> PendingValidation request
        | RequestCancelled request -> Cancelled request
        | RequestAskedToCancel request -> PendingCancelling request
        | RequestValidated request -> Validated request

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
            Ok [RequestCancelled request]
        | _ ->
            Error "Request cannot be refused"
      
    let cancelRequest request today =
        if isIn today request then
            Ok [RequestAskedToCancel request]
        else
            Ok [RequestCancelled request]

    let decide (today: DateTime) (userRequests: UserRequestsState) (user: User) (command: Command) =
        let relatedUserId = command.UserId
        match user with
        | Employee userId when userId <> relatedUserId ->
            Error "Unauthorized"
        | _ ->
            match command with
            | RequestTimeOff request ->
                let activeUserRequests =
                    userRequests
                    |> Map.toSeq
                    |> Seq.map (fun (_, state) -> state)
                    |> Seq.where (fun state -> state.IsActive)
                    |> Seq.map (fun state -> state.Request)

                createRequest today activeUserRequests request

            | CancelRequest request ->
                let requestState = defaultArg (userRequests.TryFind request.RequestId) NotCreated
                if not requestState.IsActive then
                    Error "The request is not active"
                elif user = Manager then
                    Ok [RequestCancelled request]
                elif relatedUserId <> request.UserId then
                    Error "The request should only be cancelled by the owner or a manager"
                else
                    cancelRequest request today

            | RefuseCancelationRequest (_, requestId) ->
                if user <> Manager then
                    Error "Unauthorized"
                else
                    let requestState = defaultArg (userRequests.TryFind requestId) NotCreated
                    match requestState with
                      | PendingCancelling _ -> validateRequest requestState
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
