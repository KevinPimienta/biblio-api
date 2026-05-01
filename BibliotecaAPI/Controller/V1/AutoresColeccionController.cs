using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controller.V1
{
    [ApiController]
    [Route("api/v1/autores-coleccion")]
    [Authorize(Policy = "esadmin")]
    public class AutoresColeccionController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public AutoresColeccionController(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("{ids}", Name ="ObtenerAutoresPorIdsV1")]//api/autores-coleccion/1,2,3
        public async Task<ActionResult<List<AutorConLibrosDTOs>>> Get(string ids)
        {
            var idsColeccion = new List<int>();

            foreach (var id in ids.Split(",")) // hmm yes, I see
            {
                if(int.TryParse(id, out int idInt))
                {
                    idsColeccion.Add(idInt);
                }
                
            }
            if (!idsColeccion.Any())
            {
                ModelState.AddModelError(nameof(ids), "Ningun Id fue encontrado");
                return ValidationProblem();
            }

            var autores = await context.Autores
                .Include(x => x.Libros)
                    .ThenInclude(x => x.Libro)
                    .Where(x => idsColeccion.Contains(x.Id))
                    .ToListAsync();

            if(autores.Count != idsColeccion.Count)
            {
                return NotFound();
            }
            var autoresDTO = mapper.Map<List<AutorConLibrosDTOs>>(autores);
            return autoresDTO;
        }

        [HttpPost(Name = "CrearAutoresV1")]
        public async Task<ActionResult> Post(IEnumerable<AutorCreacionDTO> autoresCreacionDTO)
        {
            var autores = mapper.Map<IEnumerable<Autor>>(autoresCreacionDTO);
            context.AddRange(autores);
            await context.SaveChangesAsync();

            var autoresDTO = mapper.Map<IEnumerable<Autor>>(autoresCreacionDTO);
            var ids = autores.Select(x => x.Id);
            var idsString = string.Join(",", ids);
            return CreatedAtRoute("ObtenerAutoresPorIdsV1", new { ids = idsString }, autoresDTO);

        }

        

    }
}
