using Super.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Super.WebApi.Controllers
{
    [System.Web.Http.RoutePrefix("api/Students")]
    public class StudentsController : ApiController
    {
        Student[] students = new Student[]
         {
            new Student { Id = 1, Name = "TomatoSoup", Age = 18 },
            new Student { Id = 2, Name = "Yoyo", Age = 19 },
            new Student { Id = 3, Name = "Hammer", Age = 20 }
         };
        [Authorize]
        [Route("")]
        public IEnumerable<Student> GetAllStudents()
        {
            return students;
        }
        [Authorize]
        public Student GetStudentById(int id)
        {
            var student = students.FirstOrDefault((p) => p.Id == id);
            if (student == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return student;
        }
    }
}