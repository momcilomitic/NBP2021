using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Neo4j.Driver;
using Neo4jClient;
using Neo4JTest.Model;

namespace Neo4JTest.Pages
{
    public class GuideCreateModel : PageModel
    {
        #region Same
        private readonly IDriver _driver;
        private BoltGraphClient _client;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GuideCreateModel(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _driver = GraphDatabase.Driver("bolt://localhost:7687/", AuthTokens.Basic("admin", "admin"));
            _client = new BoltGraphClient(_driver);
        }
        #endregion

        #region Properties
        [BindProperty]
        public Guide Guide { get; set; }
        [BindProperty(SupportsGet = true)]
        public User Korisnik { get; set; }
        [BindProperty]
        public List<IFormFile> slike { get; set; }
        #endregion

        public async Task<IActionResult> OnGet(string username)
        {
            await _client.ConnectAsync();
            Korisnik = (await _client.Cypher
                .Match("(u:User)")
                .Where((User u) => u.username == username)
                .Return(u => u.As<User>()).ResultsAsync).FirstOrDefault();

            Korisnik.guides = (await _client.Cypher
                .Match("(u:User)-[:CREATED]-(g:Guide)")
                .Where((User u) => u.username == username)
                .Return(g => g.As<Guide>()).ResultsAsync).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string username, IFormFile[] images)
        {
            await _client.ConnectAsync();

            if (ModelState.IsValid)
            {
                Guide.id = await GetMaxIdGuide() + 1;
                Guide.datumKreiranja = DateTime.Now.ToString();
            }


            await _client.Cypher
                .Match("(u:User)")
                .Where("u.username IS NOT NULL")
                .AndWhere((User u) => u.username == username)
                .Create("(g:Guide $newGuide)")
                .WithParam("newGuide", Guide)
                .Create("(u)-[:CREATED]->(g)")
                .ExecuteWithoutResultsAsync();

            foreach (IFormFile image in images)
            {
                string guidName = uploadImage(null, image);


                await _client.Cypher
                    .Match("(g:Guide)")
                    .Where("g.id IS NOT NULL")
                    .AndWhere((Guide g) => g.id == Guide.id)
                    .Create("(s:Image $par)")
                    .WithParam("par", new { uniqueName = guidName })
                    .Create("(g)-[:CONTAINS]->(s)")
                    .ExecuteWithoutResultsAsync();
            }

            return RedirectToPage("./GuideCreate", new { username = username });
        }

        public async Task<IActionResult> OnPostDelete(string username, int id)
        {
            await _client.ConnectAsync();

            await _client.Cypher
                .Match("(g:Guide)")
                .Where("g.id IS NOT NULL")
                .AndWhere((Guide g) => g.id == id)
                .Match("(g)-[:CONTAINS]-(i:Image)")
                .DetachDelete("i")
                .DetachDelete("g")
                .ExecuteWithoutResultsAsync();

            return RedirectToPage("./GuideCreate", new { username = username });
        }


        private async Task<int> GetMaxIdGuide()
        {
            await _client.ConnectAsync();

            string maxId = (await _client.Cypher
                .Match("(g:Guide)")
                .Where("g.id is not null")
                .Return<string>("max(g.id)")
                .ResultsAsync).FirstOrDefault();

            if(maxId == null)
                return 0;
            else
                return int.Parse(maxId);

        }

        private string uploadImage(string imagePathFromDb, IFormFile image)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            if (imagePathFromDb != null)
            {
                string deleteFilePath = Path.Combine(uploadsFolder, imagePathFromDb);
                if (System.IO.File.Exists(deleteFilePath))
                {
                    System.IO.File.Delete(deleteFilePath);
                }
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(fileStream);
            }
            return uniqueFileName;
        }
    }
}
