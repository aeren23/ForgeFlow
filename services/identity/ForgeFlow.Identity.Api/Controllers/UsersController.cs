using ForgeFlow.Identity.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForgeFlow.Identity.Api.Controllers;

/// <summary>
/// Kullanıcı yönetimi ve arama için controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Sadece yetkili kullanıcılar erişebilir
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Kullanıcıları arar ve listeler.
    /// Şirket içi işbirliği için kullanıcı araması.
    /// </summary>
    /// <param name="term">Arama terimi (username, email, fullname)</param>
    /// <param name="page">Sayfa numarası (default 1)</param>
    /// <param name="pageSize">Sayfa boyutu (default 10)</param>
    /// <returns>Bulunan kullanıcılar</returns>
    [HttpGet]
    public async Task<ActionResult<ListUsersResult>> Search(
        [FromQuery] string? term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new ListUsersQuery(term, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
