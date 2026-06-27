using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using backend.Controllers;
using backend.Services.Interfaces;
using backend.Models;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class RolesControllerTests
    {
        private readonly Mock<IRolService> _mockRolService;
        private readonly RolesController _controller;

        public RolesControllerTests()
        {
            _mockRolService = new Mock<IRolService>();
            _controller = new RolesController(_mockRolService.Object);
        }

        [Fact]
        public async Task GetRoles_UsuarioAdmin_DebeRetornarOk()
        {
            // Arrange
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "ADMIN" },
                new Rol { idRol = 2, nombre = "USER" }
            };
            _mockRolService.Setup(s => s.ObtenerTodosLosRoles())
                .ReturnsAsync(roles);

            // Mockear User.Claims para simular usuario ADMIN
            var claims = new List<Claim>
            {
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetRoles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(roles);
        }

        [Fact]
        public async Task GetRoles_UsuarioSuperAdmin_DebeRetornarOk()
        {
            // Arrange
            var roles = new List<Rol>
            {
                new Rol { idRol = 1, nombre = "SUPERADMIN" }
            };
            _mockRolService.Setup(s => s.ObtenerTodosLosRoles())
                .ReturnsAsync(roles);

            var claims = new List<Claim>
            {
                new Claim("rol", "SUPERADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetRoles();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetRoles_UsuarioNoAutorizado_DebeRetornarForbid()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim("rol", "USER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetRoles();

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetRoles_SinClaims_DebeRetornarForbid()
        {
            // Arrange
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetRoles();

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetRoles_CuandoOcurreError_DebeRetornarServerError()
        {
            // Arrange
            _mockRolService.Setup(s => s.ObtenerTodosLosRoles())
                .ThrowsAsync(new Exception("Error de base de datos"));

            var claims = new List<Claim>
            {
                new Claim("rol", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.GetRoles();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}
