using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Biblioteca.Data;
using Biblioteca.Models;

namespace Biblioteca.Controllers
{
    using Microsoft.AspNetCore.Identity; // Adicione este using

    public class AvaliacoesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AvaliacoesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Avaliacoes
        public async Task<IActionResult> Index(int? livroId)
        {
            // Obtém o usuário logado
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.AppUserId == Guid.Parse(user.Id));
            if (usuario == null)
                return Challenge();

            // Livros que o usuário já retirou e devolveu
            var livrosRetirados = await _context.Movimentacoes
                .Where(m => m.UsuarioId == usuario.UsuarioId && m.LivroDevolvido)
                .Select(m => m.Livro)
                .Distinct()
                .ToListAsync();

            // Se livroId foi passado, filtra para avaliação desse livro
            ViewBag.LivroId = livroId;
            ViewBag.LivrosRetirados = livrosRetirados;

            // Lista avaliações do usuário logado
            var avaliacoes = await _context.Avaliacoes
                .Include(a => a.Livro)
                .Where(a => a.UsuarioId == usuario.UsuarioId)
                .ToListAsync();

            return View(avaliacoes);
        }

        // GET: Avaliacoes/Create
        public async Task<IActionResult> Create(int? livroId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.AppUserId == Guid.Parse(user.Id));
            if (usuario == null)
                return Challenge();

            // Livros que o usuário já retirou e devolveu
            var livrosRetirados = await _context.Movimentacoes
            .Where(m => m.UsuarioId == usuario.UsuarioId)
            .Select(m => m.Livro)
            .Distinct()
            .ToListAsync();

            ViewData["LivroId"] = new SelectList(livrosRetirados, "LivroId", "Titulo", livroId);
            return View();
        }

        // POST: Avaliacoes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nota,Comentario,LivroId")] Avaliacao avaliacao)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.AppUserId == Guid.Parse(user.Id));
            if (usuario == null)
                return Challenge();

            if (ModelState.IsValid)
            {
                avaliacao.UsuarioId = usuario.UsuarioId;
                avaliacao.DataAvaliacao = DateTime.Now;
                _context.Add(avaliacao);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Recarrega os livros retirados para o select
            var livrosRetirados = await _context.Movimentacoes
                .Where(m => m.UsuarioId == usuario.UsuarioId && m.LivroDevolvido)
                .Select(m => m.Livro)
                .Distinct()
                .ToListAsync();

            ViewData["LivroId"] = new SelectList(livrosRetirados, "LivroId", "Titulo", avaliacao.LivroId);
            return View(avaliacao);
        }
    }

}
