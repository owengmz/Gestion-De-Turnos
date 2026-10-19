
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Models;

namespace mvc.Controllers
{
    [Authorize]
    public class TurnosController : Controller
    {
        private readonly AppDbContext _context;

        public TurnosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Turnos.AsQueryable();

            if (User.IsInRole(Rol.Profesional.ToString()))
            {
                var professionalIdValue = User.FindFirstValue("ProfesionalId");
                if (!int.TryParse(professionalIdValue, out var professionalId))
                {
                    return Forbid();
                }

                query = query.Where(turno => turno.profesional_id == professionalId);
            }
            else if (!User.IsInRole(Rol.Administrador.ToString())
                && !User.IsInRole(Rol.Recepcionista.ToString()))
            {
                return Forbid();
            }

            var turnos = await query
                .Include(t => t.Paciente)
                .Include(t => t.Profesional)
                .OrderBy(t => t.fecha)
                .ThenBy(t => t.hora)
                .ToListAsync();

            return View(turnos);
        }
    }
}
