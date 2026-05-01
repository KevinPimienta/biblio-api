using BibliotecaAPI.Validaciones;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaAPITest.PruebasUnitarias.Validaciones
{
    [TestClass]
    public class PrimeraMayusculaAttributePruebas
    {
        [TestMethod]
        [DataRow("")]
        [DataRow("    ")]
        [DataRow(null)]
        public void IsValid_RetornaExitoso_SiValueEsVacioONulo(string value)
        {
            // Preparacion
            var primeraLetraMayusculaAttribute = new PrimeraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());
            //  var value = string.Empty; <-- Sin el uso de DataRow, se quita el string value que llega y tambien la variable.
            // Pruebas
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            // Verificacion
            Assert.AreEqual(expected: ValidationResult.Success, actual: resultado);
        }

        [TestMethod]
        [DataRow("Felipe")]
        public void IsValid_RetornaExitoso_SiLaPrimeraLetraEsMayuscula(string value)
        {
            // Preparacion
            var primeraLetraMayusculaAttribute = new PrimeraMayusculaAttribute();
            var validationContext = new ValidationContext(new object());
            //  var value = string.Empty; <-- Sin el uso de DataRow, se quita el string value que llega y tambien la variable.
            // Pruebas
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext);

            // Verificacion
            Assert.AreEqual(expected: ValidationResult.Success, actual: resultado);
        }
    }
}
