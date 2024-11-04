namespace MinimalApi.DTOs;
public class LoginDTO
{
    public String Email { get; set; } = default!;
    public String Senha { get; set; } = default!;
}