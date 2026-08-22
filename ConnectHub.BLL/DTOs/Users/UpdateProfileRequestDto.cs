namespace ConnectHub.BLL.DTOs.Users;

public class UpdateProfileRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
}
