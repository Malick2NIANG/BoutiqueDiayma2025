using System.Linq;
using Microsoft.AspNetCore.Mvc;
using P2FixAnAppDotNetCode.Models;
using P2FixAnAppDotNetCode.Models.Services;

namespace P2FixAnAppDotNetCode.Controllers
{
    public class CartController : Controller
    {
        private readonly ICart _cart;
        private readonly IProductService _productService;

        public CartController(ICart pCart, IProductService productService)
        {
            _cart = pCart;
            _productService = productService;
        }

        public ViewResult Index()
        {
            return View(_cart as Cart);
        }

        [HttpPost]
        public RedirectToActionResult AddToCart(int id, string returnTo = null)
        {
            Product product = _productService.GetProductById(id);

            if (product != null)
            {
                // Correction : on n'ajoute pas plus d'articles qu'il n'y en a en stock
                int quantiteDansPanier = ((Cart)_cart).Lines
                    .Where(l => l.Product.Id == product.Id)
                    .Sum(l => l.Quantity);
                bool ajoute = quantiteDansPanier < product.Stock;

                if (ajoute)
                {
                    _cart.AddItem(product, 1);
                }

                // Bouton "+" du panier : on reste sur le panier
                if (returnTo == "cart")
                {
                    return RedirectToAction("Index");
                }

                // Ergonomie : l'utilisateur reste sur la liste des produits avec un message de confirmation
                TempData["CartMessage"] = ajoute ? "added" : "stock";
                TempData["CartProduct"] = product.Name.Trim();
                return RedirectToAction("Index", "Product");
            }
            else
            {
                return RedirectToAction("Index", "Product");
            }
        }

        public RedirectToActionResult RemoveFromCart(int id)
        {
            // Correction : on cherche le produit dans le panier (un produit dont le stock
            // est tombé à 0 n'apparaît plus dans GetAllProducts et ne pouvait plus être retiré)
            Product product = ((Cart)_cart).Lines
                .Select(l => l.Product)
                .FirstOrDefault(p => p.Id == id);

            if (product != null)
            {
                _cart.RemoveLine(product);
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Diminue de 1 la quantité d'un produit du panier (bouton "-") ; retire la ligne à 0
        /// </summary>
        [HttpPost]
        public RedirectToActionResult DecreaseQuantity(int id)
        {
            CartLine ligne = ((Cart)_cart).Lines.FirstOrDefault(l => l.Product.Id == id);

            if (ligne != null)
            {
                if (ligne.Quantity > 1)
                {
                    ligne.Quantity--;
                }
                else
                {
                    _cart.RemoveLine(ligne.Product);
                }
            }
            return RedirectToAction("Index");
        }
    }
}
