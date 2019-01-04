/// Module of server domain types.
module ServerCode.ServerTypes

/// Represents the authenticated user for a request
type Identity =
    {
        UserName : string
        UserId: int
        Roles: string list
    }