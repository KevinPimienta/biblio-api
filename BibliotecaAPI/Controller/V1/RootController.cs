using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controller.V1
{
    [ApiController]
    [Route("api/v1")]
    public class RootController: ControllerBase
    {
        [HttpGet(Name = "ObtenerRootV1")]
        public IEnumerable<DatosHATEOSDTO> Get()
        {
            var datosHATEOS = new List<DatosHATEOSDTO>();

            datosHATEOS.Add(new DatosHATEOSDTO(Enlace: Url.Link("ObtenerRootV1", new {})!,
                 Descripcion: "Self", Metodo: "GET"));

            return datosHATEOS;//Que es todo esto???
        }
    }
}
