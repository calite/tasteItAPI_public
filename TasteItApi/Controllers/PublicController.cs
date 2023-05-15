using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using Neo4jClient;
using System.Globalization;
using System.Text;
using TasteItApi.Models;


namespace TasteItApi.Controllers
{
    public class PublicController : Controller
    {
        private readonly IGraphClient _client;

        //DOC: https://github.com/DotNet4Neo4j/Neo4jClient/wiki

        // Diccionario de palabras más usadas en las recetas
        string[] commonWords = new string[] {
                    "sal", "azucar", "aceite", "cebolla",
                    "ajo", "tomate", "pollo", "carne", "pescado",
                    "arroz", "pasta", "huevo", "huevos", "leche", "harina",
                    "pan", "queso", "mayonesa", "mostaza", "vinagre",
                    "limon", "naranja", "manzana", "platano", "fresa",
                    "chocolate", "vainilla", "canela", "nuez", "mantequilla",
                    "crema", "almendra", "cacahuete", "mermelada", "miel",
                    "jengibre", "curry", "pimienta", "salvia", "romero",
                    "oregano", "laurel", "tomillo", "perejil", "cilantro",
                    "menta", "albahaca", "salsa", "sopa", "ensalada",
                    "guiso", "horneado", "frito", "asado", "cocido", "microondas" };


        public PublicController(IGraphClient client)
        {
            _client = client;
        }

        [HttpGet("/public/recipes/all")]
        public async Task<ActionResult<Recipe>> GetAllRecipes()
        {
            //devuelve las recetas seguido del usuario que la creo
            var result = await _client.Cypher
                .Match("(recipe:Recipe)")
                .Return((recipe) => new
                {
                    RecipeId = recipe.Id(),
                    Recipe = recipe.As<Recipe>()
                })
                .OrderBy("recipe.dateCreated desc")
                .ResultsAsync;

            var results = result.ToList();

            //if (results.Count == 0)
            //{
            //    return NotFound();
            //}

            return Ok(results);
        }

        [HttpGet("/public/recipes/all/{skipper:int}")]
        public async Task<ActionResult<Recipe>> GetAllRecipesWithSkipper(int skipper)
        {
            //devuelve las recetas seguido del usuario que la creo
            var result = await _client.Cypher
                .Match("(recipe:Recipe)")
                .Return((recipe) => new
                {
                    RecipeId = recipe.Id(),
                    Recipe = recipe.As<Recipe>(),
                })
                .OrderBy("recipe.dateCreated desc")
                .Skip(skipper)
                .Limit(10)
                .ResultsAsync;

            var results = result.ToList();

            return Ok(results);
        }

        [HttpGet("/public/recipes/{id:int}")]
        public async Task<ActionResult<Recipe>> GetRecipeById(int id)
        {
            var result = await _client.Cypher
                .Match("(recipe:Recipe)")
                .Where("ID(recipe) = " + id)
                .Return((recipe, user) => new
                {
                    RecipeId = recipe.Id(),
                    Recipe = recipe.As<Recipe>()
                })
                .ResultsAsync;

            //var results = result.ToList();

            return Ok(result);
        }

        [HttpGet("/public/recipes/random/{limit}")]
        public async Task<ActionResult<Recipe>> GetRandomRecipesWithLimit(int limit)
        {

            //devuelve un numero aleatorio de recetas seguido del usuario que la creo
            var result = await _client.Cypher
                .Match("(recipe:Recipe)")
                .With("recipe, rand() as rand")
                .OrderBy("rand limit $limit")
                .Match("(recipe:Recipe)")
                .WithParam("limit", limit)
                .Return((recipe) => new
                {
                    RecipeId = recipe.Id(),
                    Recipe = recipe.As<Recipe>()
                })
                .ResultsAsync;

            var results = result.ToList();

            return Ok(results);
        }


        [HttpGet("/public/recipes/byname/{name}/{skipper}")]
        public async Task<ActionResult<Recipe>> GetRecipeByName(string name, int skipper)
        {
            //devuelve recetas filtrando por nombre seguido del usuario que la creo
            var result = await _client.Cypher
                            .Match("(recipe:Recipe)-[:Created]-(user:User)")
                            .Where("toLower(recipe.name) CONTAINS toLower($name)")
                            .WithParam("name", name)
                            .Return((recipe) => new
                            {
                                RecipeId = recipe.Id(),
                                Recipe = recipe.As<Recipe>()
                            })
                            .OrderBy("recipe.dateCreated desc")
                            .Skip(skipper)
                            .Limit(10)
                            .ResultsAsync;

            var recipe = result.ToList();

            return Ok(recipe);
        }

        [HttpGet("/public/recipes/bycountry/{country}/{skipper}")]
        public async Task<ActionResult<Recipe>> GetRecipeByCountry(string country, int skipper)
        {
            //filtramos recetas por ciudad seguido del usuario que la creo
            var result = await _client.Cypher
                            .Match("(recipe:Recipe)")
                            .Where("toLower(recipe.country) CONTAINS toLower($country)")
                            .WithParam("country", country)
                            .Return((recipe) => new
                            {
                                RecipeId = recipe.Id(),
                                Recipe = recipe.As<Recipe>()
                            })
                           .OrderBy("recipe.dateCreated desc")
                           .Skip(skipper)
                           .Limit(10)
                           .ResultsAsync;

            var recipe = result.ToList();

            return Ok(recipe);
        }

        //ARREGLAR el skipper
        [HttpGet("/public/recipes/byingredients/{ingredients}/{skipper}")]
        public async Task<ActionResult<List<Recipe>>> GetRecipeByIngredients(string ingredients, int skipper)
        {

            List<string> listIng = ingredients.Replace(" ", "").Split(",").ToList();

            var result = await _client.Cypher
                            .Match("(recipe:Recipe)")
                            .Return((recipe) => new
                            {
                                RecipeId = recipe.Id(),
                                Recipe = recipe.As<Recipe>(),
                            })
                            .OrderBy("recipe.dateCreated desc")
                            .ResultsAsync;

            var recipes = result.ToList();

            //Dictionary<Recipe, User> listRecipesFiltered = new Dictionary<Recipe, User>();
            List<Object> listRecipesFiltered = new List<Object>();

            for (int i = 0; i < recipes.Count; i++)
            {
                //miramos si la lista de los ingredientes contiene alguno de los elementos de los introducidos por el usuario
                bool hasMatch = recipes[i].Recipe.ingredients.Any(x => listIng.Any(y => x.ToLower().Contains(y.ToLower())));

                if (hasMatch)
                {
                    listRecipesFiltered.Add(new {
                        recipes[i].RecipeId,
                        Recipe = recipes[i].Recipe.As<Recipe>()
                    });
                }

            }

            return Ok(listRecipesFiltered.ToList());
        }

        //ARREGLAR el skipper
        [HttpGet("/public/recipes/bytags/{tags}/{skipper}")]
        public async Task<ActionResult<List<Recipe>>> GetRecipeByTags(string tags, int skipper)
        {

            List<string> listTags = tags.Replace(" ", "").Split(",").ToList();

            var result = await _client.Cypher
                            .Match("(recipe:Recipe)")
                            .Return((recipe) => new
                            {
                                RecipeId = recipe.Id(),
                                Recipe = recipe.As<Recipe>()
                            })
                            .OrderBy("recipe.dateCreated desc")
                            .ResultsAsync;

            var recipes = result.ToList();

            List<Object> listRecipesFiltered = new List<Object>();

            for (int i = 0; i < recipes.Count; i++)
            {
                //miramos si la lista de los ingredientes contiene alguno de los elementos de los introducidos por el usuario
                bool hasMatch = recipes[i].Recipe.tags.Any(x => listTags.Any(y => y.ToLower() == x.ToLower()));

                if (hasMatch)
                {
                    listRecipesFiltered.Add(new
                    {
                        recipes[i].RecipeId,
                        Recipe = recipes[i].Recipe.As<Recipe>()
                    });
                }

            }

            return Ok(listRecipesFiltered.ToList());
        }

    }
}
