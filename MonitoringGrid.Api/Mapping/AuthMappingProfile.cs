using AutoMapper;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Api.Mapping;

/// <summary>
/// AutoMapper profile for authentication-related mappings
/// </summary>
public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        // Authentication request/response mappings
        CreateMap<LoginRequestDto, LoginRequest>();
        CreateMap<LoginResponse, LoginResponseDto>();
        CreateMap<JwtToken, JwtTokenDto>();
        CreateMap<ChangePasswordRequestDto, ChangePasswordRequest>();

        // User entity mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role)))
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => 
                src.UserRoles.SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.GetFullPermissionName()))))
            .ReverseMap();

        CreateMap<User, AuthUser>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => 
                src.UserRoles.SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.GetFullPermissionName())).ToList()))
            .ReverseMap();

        // Role entity mappings
        CreateMap<MonitoringGrid.Core.Entities.Role, RoleDto>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.Permission)))
            .ReverseMap();

        // Permission entity mappings
        CreateMap<MonitoringGrid.Core.Entities.Permission, PermissionDto>()
            .ReverseMap();

        // User management request mappings
        CreateMap<CreateUserRequestDto, CreateUserRequest>()
            .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.RoleIds))
            .ReverseMap();

        CreateMap<UpdateUserRequestDto, UpdateUserRequest>()
            .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.RoleIds))
            .ReverseMap();

        CreateMap<RegisterRequestDto, CreateUserRequest>()
            .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => new List<string> { "role-viewer" })) // Default role
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => "SYSTEM"));

        // User password mappings
        CreateMap<UserPassword, UserPasswordDto>()
            .ReverseMap();

        // Refresh token mappings
        CreateMap<RefreshToken, RefreshTokenDto>()
            .ReverseMap();

        // Security event mappings
        CreateMap<SecurityAuditEvent, SecurityEventDto>()
            .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src =>
                src.AdditionalData.ContainsKey("Description") ? src.AdditionalData["Description"].ToString() : ""))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")))
            .ReverseMap();

        // API key mappings
        CreateMap<ApiKey, ApiKeyDto>()
            .ReverseMap();
    }
}

/// <summary>
/// User password DTO for password history
/// </summary>
public class UserPasswordDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Refresh token DTO
/// </summary>
public class RefreshTokenDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
    public string? RevokedReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
