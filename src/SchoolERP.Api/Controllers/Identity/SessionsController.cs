using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Identity.Sessions;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Identity;

/// <summary>
/// Lets the browser report that the user is still active when they haven't needed the
/// server for a while (reading a page, filling in a long form). Validating the token already
/// records the activity, so this endpoint only has to answer.
///
/// Deliberately not on AuthController: that one is limited to 60 requests a minute per
/// network address, which a whole school on one Wi-Fi would exhaust with heartbeats. This
/// falls under the normal per-user limit instead.
/// </summary>
[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessions;

    public SessionsController(ISessionService sessions) => _sessions = sessions;

    [HttpPost("heartbeat")]
    public IActionResult Heartbeat() =>
        Ok(ApiResponse<object>.Ok(new { idleTimeoutMinutes = _sessions.IdleTimeoutMinutes }));
}
