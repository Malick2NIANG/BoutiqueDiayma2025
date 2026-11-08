# Boutique Diayma — TD Visual Studio & Débogage

**ESP / UCAD — Département Génie Informatique — M2 GLSI 2026/2027**
TD C# et Technologies .NET (E. H. Ousmane Diallo) — 03 novembre 2026
Étudiant : Malick NIANG

Dépôt d'origine : https://github.com/elhadjiousmane/BoutiqueDiayma2025

---

## 1. Récupération et exécution

Clonage dans Visual Studio 2026 : *Fichier > Cloner un référentiel*, URL du dépôt ci-dessus, puis ouverture de `Diayma.sln` et exécution (F5). À l'ouverture, Visual Studio affiche une fenêtre d'avertissements ⚠ : la cible `netcoreapp2.0` n'est plus prise en charge (fin de vie le 1er octobre 2018). On la ferme pour poursuivre.

## 2. Projets de la solution

La solution **`Diayma.sln`** contient **un seul projet** :

| Projet | Fichier | Dossier | Type |
|---|---|---|---|
| `Diayma` | `Diayma.csproj` | `P2FixAnAppDotNetCode/` | Application web ASP.NET Core MVC (SDK `Microsoft.NET.Sdk.Web`) |

L'espace de noms racine du code est `P2FixAnAppDotNetCode`. Organisation : `Controllers/` (Product, Cart, Order, Language), `Components/` (ViewComponents CartSummary et LanguageSelector), `Models/` (Cart, Product, Order, `Services/`, `Repositories/`, `ViewModels/`), `Views/` (Razor), `Resources/` (fichiers `.resx` de traduction en/fr).

## 3. Version du SDK .NET utilisée

- `Diayma.csproj` : `<TargetFramework>netcoreapp2.0</TargetFramework>` → **.NET Core 2.0**
- Paquets : `Microsoft.AspNetCore.All` **2.0.6**, `Microsoft.Extensions.Localization` 2.1.1
- Pas de `global.json` : aucune version de SDK n'est imposée.

Le SDK qui correspond est le **.NET Core 2.0 SDK, version 2.1.202** (le dernier de la série, avec les runtimes .NET Core 2.0.9 et ASP.NET Core 2.0.9).

## 4. Installation du SDK

1. Télécharger le **SDK 2.1.202 (Windows x64)** depuis https://dotnet.microsoft.com/download/dotnet/2.0.
2. Installer, puis vérifier dans un terminal :
   ```
   dotnet --list-sdks      # doit afficher 2.1.202
   dotnet --list-runtimes  # Microsoft.NETCore.App 2.0.9 / Microsoft.AspNetCore.All 2.0.9
   ```
3. Rouvrir la solution et lancer *Générer > Régénérer la solution*.

## 5. Dépôt GitHub personnel

Mon dépôt : https://github.com/Malick2NIANG/BoutiqueDiayma2025

## 6. Bugs trouvés

### Bug n° 1 : le total du panier ignore les quantités

- **Reproduction** : ajouter deux fois le *XIAOMI Redmi 13C* (88 000) au panier. La ligne indique Quantité = 2 et Sous-total = 176 000, mais le **Total affiche 88 000**. Le résumé du panier dans la barre du haut est faux aussi.
- **Cause** : `Models/Cart.cs`, `GetTotalValue()` → `Sum(x => x.Product.Price)` additionne les prix unitaires sans multiplier par la quantité.
- **Correction** : `Sum(x => x.Product.Price * x.Quantity)`.
- **Bug lié** : `GetAverageValue()` fait la moyenne des prix *par ligne* au lieu de diviser le total par le nombre d'articles. Il est corrigé de la même façon : `GetTotalValue() / Σ quantités`.

### Bug n° 2 : le stock ne diminue jamais après une commande

- **Reproduction** : commander 2 *Clé USB 64GB* (stock 20), valider la commande, puis revenir à la liste des produits. Le stock affiche toujours **20** au lieu de 18.
- **Cause** : `Models/Repositories/ProductRepository.cs`. La liste `_products` est **`static`**, mais le **constructeur** la recrée et la remplit à nouveau à chaque instanciation. Or `ProductRepository` est enregistré en **`AddTransient`** dans `Startup`, donc une nouvelle instance est créée à chaque requête. La diminution du stock faite par `UpdateProductStocks()` est perdue dès la requête suivante.
- **Correction** : ne générer les données que si `_products == null`. Dans `UpdateProductStocks`, on retire aussi le produit quand le stock est `<= 0`, et plus seulement `== 0`.

### Autres anomalies relevées

| # | Anomalie | Emplacement |
|---|---|---|
| 3 | Les messages d'erreur du formulaire de commande s'affichent sous forme de clés brutes (`ErrorMissingName`…) au lieu du texte traduit. Les `.resx` de la classe `Order` sont rangés dans `Resources/Models/ViewModels/` alors que `Order` est dans l'espace de noms `Models` (chemin attendu : `Resources/Models/Order.*.resx`). En plus, le `.csproj` exclut `Order.en.resx` des ressources incorporées. | `Resources/`, `Diayma.csproj` |
| 4 | `FindProductInCartLines()` lève une `NullReferenceException` si le produit n'est pas dans le panier (`FirstOrDefault().Product`). | `Models/Cart.cs` |
| 5 | Le choix « Espagnol » ne change rien : la culture `es` n'est pas déclarée dans `SupportedCultures` et il n'existe aucun fichier `.es.resx`. | `Startup.cs`, `Resources/` |
| 6 | On peut ajouter au panier plus d'articles qu'il n'y en a en stock : aucun contrôle dans `AddToCart`. | `CartController.cs` |
| 7 | Le panier est un **Singleton**, donc partagé par tous les visiteurs du site. Pour une vraie boutique, il faudrait un panier par session (`AddSession` est configuré mais pas utilisé pour le panier). | `Startup.cs` l. 29 |
| 8 | Les prix sont affichés en **euros (€)** ou avec le symbole de la culture du navigateur (`ToString("c")`), alors que les prix du catalogue sont en **francs CFA** : une clé USB affichée à « 1 000,00 € ». | Vues `Product/Index`, `Cart/Index`, `CartSummary` |

Corrections apportées :
- anomalies 1 à 4 : commit « Correction des bugs » ;
- anomalies 5, 6 et 8 : commit « Devise FCFA, contrôle du stock et langue espagnole ». Les prix s'affichent en FCFA (`ToString("N0") + " FCFA"`). `AddToCart` refuse d'ajouter au panier plus d'articles que le stock disponible. La culture `es` est déclarée et des fichiers `.es.resx` traduisent l'interface en espagnol.

L'anomalie 7 (panier partagé entre tous les visiteurs) n'est pas corrigée : il faudrait stocker le panier dans la session de chaque utilisateur, ce qui dépasse le cadre du TD.

## 7. Points d'arrêt

| Fichier | Ligne | Instruction | Quand le point d'arrêt est atteint |
|---|---|---|---|
| `Components/CartSummaryViewComponent.cs` | 12 | `_cart = cart as Cart;` (constructeur) | À chaque page, au rendu du résumé du panier dans `_Layout` |
| `Controllers/ProductController.cs` | 15 | `_productService = productService;` (constructeur) | À chaque affichage de la liste des produits (page d'accueil) |
| `Controllers/OrderController.cs` | 17 | `_cart = pCart;` (constructeur) | Au clic sur « Passer la commande » (`/Order/Index`) |
| `Controllers/CartController.cs` | 15 | `_cart = pCart;` (constructeur) | Au clic sur « Ajouter au panier », à l'affichage ou à la suppression dans le panier |
| `Startup.cs` | 20 | `Configuration = configuration;` (constructeur) | **Une seule fois**, au démarrage de l'hôte web |

Les cinq points d'arrêt sont placés dans des **constructeurs**. Ces objets sont créés par le **conteneur d'injection de dépendances** d'ASP.NET Core, pas par notre code. C'est pourquoi la **Pile des appels** montre surtout des méthodes du framework (`Microsoft.AspNetCore.*`, `Microsoft.Extensions.DependencyInjection.*`) entre deux méthodes à nous.

## 8. Parcours jusqu'à l'affichage des produits sur la page d'accueil

Raccourcis : **F11 = Pas à pas détaillé**, **F10 = Pas à pas principal**, **Maj+F11 = Pas à pas sortant**, **F5 = Continuer**. L'option *Uniquement mon code* est activée : le débogueur saute le code du framework.

### Étape A : démarrage de l'application (une seule fois)

| # | Namespace | Classe | Méthode | Mode choisi et justification |
|---|---|---|---|---|
| 1 | `P2FixAnAppDotNetCode` | `Program` | `Main(string[] args)` | **F11** sur `BuildWebHost(args).Run()` pour entrer dans `BuildWebHost` |
| 2 | `P2FixAnAppDotNetCode` | `Program` | `BuildWebHost(string[] args)` | **F10** : `WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build()` est du code framework (Kestrel, configuration, logs) qu'il est inutile de détailler |
| 3 | `P2FixAnAppDotNetCode` | `Startup` | `Startup(IConfiguration)` (**point d'arrêt l. 20**) | L'hôte instancie `Startup` pendant `Build()`. **F10** |
| 4 | `P2FixAnAppDotNetCode` | `Startup` | `ConfigureServices(IServiceCollection)` | **F10** ligne par ligne pour voir les services enregistrés : `ICart → Cart` (Singleton), `ILanguageService → LanguageService` (Singleton), `IProductService → ProductService`, `IProductRepository → ProductRepository`, `IOrderService → OrderService`, `IOrderRepository → OrderRepository` (Transient), localisation, mémoire cache, session, MVC |
| 5 | `P2FixAnAppDotNetCode` | `Startup` | `Configure(IApplicationBuilder, IHostingEnvironment)` | **F10** : construction du pipeline : fichiers statiques → localisation (`RequestLocalizationOptions`) → session → MVC avec la route par défaut `{controller=Product}/{action=Index}/{id?}` |
| 6 | `P2FixAnAppDotNetCode` | `Program` | `Main` → `Run()` | **Maj+F11** pour remonter dans `Main`, puis **F5** : Kestrel démarre et le navigateur s'ouvre sur `/` (`launchBrowser: true`) |

### Étape B : requête `GET /` (page d'accueil)

La route par défaut envoie `/` vers **`ProductController.Index`**. Avant d'appeler le contrôleur, le conteneur d'injection construit ses dépendances :

| # | Namespace | Classe | Méthode | Mode choisi et justification |
|---|---|---|---|---|
| 7 | `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` | `ProductRepository()` puis `GenerateProductData()` | Créé par l'injection de dépendances, sans appel visible depuis notre code. Pour le voir, il faut un point d'arrêt supplémentaire dans le constructeur, puis **F11** sur `GenerateProductData()` (9 produits ajoutés) |
| 8 | `P2FixAnAppDotNetCode.Models.Repositories` | `OrderRepository` | `OrderRepository()` | Idem. **F10** |
| 9 | `P2FixAnAppDotNetCode.Models.Services` | `ProductService` | `ProductService(IProductRepository, IOrderRepository)` | Idem. **F10** |
| 10 | `P2FixAnAppDotNetCode.Models.Services` | `LanguageService` | (constructeur par défaut) | Singleton créé à la première requête, sans code à suivre |
| 11 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | `ProductController(IProductService, ILanguageService)` (**point d'arrêt l. 15**) | **F10** jusqu'à la fin du constructeur, puis **F5** ou **F11** pour arriver dans l'action |
| 12 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | `Index()` | **F11** sur `_productService.GetAllProducts()` pour suivre la récupération des données |
| 13 | `P2FixAnAppDotNetCode.Models.Services` | `ProductService` | `GetAllProducts()` | **F11** sur `_productRepository.GetAllProducts()` |
| 14 | `P2FixAnAppDotNetCode.Models.Repositories` | `ProductRepository` | `GetAllProducts()` | **F10** : filtre `Stock > 0`, tri par `Name`, conversion en tableau. **Maj+F11** pour sortir |
| 15 | `P2FixAnAppDotNetCode.Models.Services` | `ProductService` | `GetAllProducts()` | `.ToList()` (tableau → `List<Product>`). **Maj+F11** |
| 16 | `P2FixAnAppDotNetCode.Controllers` | `ProductController` | `Index()` | **F10** sur `return View(products);` : le contrôleur renvoie un `ViewResult` |

### Étape C : rendu de la vue (Razor)

| # | Namespace | Classe | Méthode | Mode choisi et justification |
|---|---|---|---|---|
| 17 | `AspNetCore` | `_Views_Product_Index` (`Views/Product/Index.cshtml`) | `ExecuteAsync()` | La vue s'exécute en premier : tableau des produits (`foreach (Product p in Model)`) et textes traduits par `IViewLocalizer`. On peut placer un point d'arrêt dans le `.cshtml` et avancer en **F10** |
| 18 | `AspNetCore` | `_Views_Shared__Layout` (`Views/Shared/_Layout.cshtml`) | `ExecuteAsync()` | Le layout (défini par `_ViewStart.cshtml`) entoure le contenu (`@RenderBody()`) et appelle `@await Component.InvokeAsync("CartSummary")` dans l'en-tête |
| 19 | `P2FixAnAppDotNetCode.Models` | `Cart` | (constructeur implicite, initialisation de `_lignesPanier`) | Singleton créé à la première utilisation |
| 20 | `P2FixAnAppDotNetCode.Components` | `CartSummaryViewComponent` | `CartSummaryViewComponent(ICart)` (**point d'arrêt l. 12**) puis `Invoke()` | **F10** puis **F11** dans `Invoke()` → `View(_cart)` → `Views/Shared/Components/CartSummary/Default.cshtml` (lien « Panier » avec le nombre d'articles) |
| 21 | `P2FixAnAppDotNetCode.Components` | `LanguageSelectorViewComponent` | `Invoke(ILanguageService)` | Appelé dans l'en-tête de `_Layout`, à droite du panier → `Views/Shared/Components/LanguageSelector/Default.cshtml` (liste des langues). **F10**, puis **F5** : la page s'affiche dans le navigateur |

**À retenir** : sur la page d'accueil, seuls les points d'arrêt de `Startup` (l. 20, une fois), `ProductController` (l. 15) et `CartSummaryViewComponent` (l. 12) sont atteints. Ceux de **`CartController` (l. 15)** et **`OrderController` (l. 17)** ne le sont que plus tard : au clic sur *Ajouter au panier* (`POST /Cart/AddToCart`) et sur *Passer la commande* (`/Order/Index`).

## 9. Déploiement sous forme d'exécutable Windows

Le profil `Properties/PublishProfiles/FolderProfile.pubxml` est déjà configuré en **autonome** (`SelfContained = true`) pour **`win-x64`**. Seul le dossier cible `F:\Diayma` est à changer.

- **Avec Visual Studio** : clic droit sur le projet → *Publier…* → profil *FolderProfile* → modifier l'emplacement cible → *Publier*.
- **En ligne de commande** :
  ```
  cd P2FixAnAppDotNetCode
  dotnet publish -c Release -r win-x64 --self-contained -o publish
  ```

Le dossier `publish/` contient **`Diayma.exe`**. Un double-clic démarre le serveur web, puis on ouvre l'adresse affichée dans la console (par défaut http://localhost:5000). Le dossier est compressé en `.zip` avant d'être déposé en ligne.

## 10. Lien vers l'exécutable

*[lien Google Drive / OneDrive vers Diayma-win-x64.zip]*

## 11. Partie optionnelle

### a) Ajout du wolof (le français est conservé)

- `Models/Services/LanguageService.cs` : `SetCulture("Wolof")` renvoie la culture `"wo"`.
- `Startup.cs` : ajout de `wo-SN` et `wo` aux `SupportedCultures` / `SupportedUICultures`. Les cultures `fr` et `fr-FR` restent en place.
- `Views/Shared/Components/LanguageSelector/Default.cshtml` : nouvelle option *Wolof*.
- `Resources/**/*.wo.resx` : traduction en wolof de toutes les vues, des messages de validation et du contrôleur `Order`. La clé `LanguageWolof` est ajoutée aux fichiers `.en.resx` et `.fr.resx`.

### b) Commits

1. **Correction des bugs** : total et moyenne du panier, stock non mis à jour, ressources de validation de `Order`, `NullReferenceException`.
2. **Ajout de la langue wolof** : culture `wo`, sélecteur, fichiers `.wo.resx`.
3. **README** : réponses du TD.
4. **Devise FCFA, contrôle du stock et langue espagnole** : anomalies 5, 6 et 8.
5. **Refonte de l'interface selon les critères de Bastien et Scapin** : voir la section 12.

### c) Dépôt du lien GitHub sur Google Classroom

## 12. Ergonomie : refonte selon les critères de Bastien et Scapin

L'interface d'origine posait plusieurs problèmes :
- fond beige peu lisible ;
- prix centrés et affichés en euros ;
- icône de panier sans libellé ;
- renvoi automatique vers le panier à chaque ajout ;
- suppression sans confirmation ;
- formulaire sans indication des champs obligatoires ni messages par champ ;
- page de fin demandant de « cliquer sur le logo pour recommencer ».

Elle a été reprise selon les 8 critères de Bastien et Scapin (INRIA, 1993).

| Critère | Sous-critère | Mise en œuvre dans Boutique Diayma |
|---|---|---|
| **1. Guidage** | Incitation | Titre et phrase d'introduction sur chaque page. Barre d'étapes *Panier → Livraison → Confirmation*. Exemples de saisie dans les champs (« ex. : Dakar »). |
| | Groupement / distinction | En-tête (marque, navigation, panier, choix de la langue), contenu, pied de page. Nom et description du produit regroupés. Montants alignés à droite. Récapitulatif de commande séparé du formulaire. |
| | Feedback immédiat | Message « « Clé USB… » a été ajouté à votre panier » après chaque ajout. Compteur d'articles et total dans l'en-tête. Mention « n dans votre panier » sur chaque produit. |
| | Lisibilité | Fond clair, texte foncé bien contrasté, police système, chiffres alignés, prix séparés par milliers (`88 000 FCFA`). |
| **2. Charge de travail** | Brièveté | Boutons **−** / **+** pour changer une quantité directement dans le panier. Pays pré-rempli (« Sénégal »). Saisie automatique du navigateur (`autocomplete`). |
| | Densité informationnelle | Colonnes réduites à l'essentiel (Produit, Prix, Disponibilité, Action). Le prix moyen est relégué en petit sous le total. |
| **3. Contrôle explicite** | Actions explicites | Aucune action automatique : l'ajout laisse l'utilisateur sur la liste. Changement de langue déclenché uniquement par un clic sur la langue voulue. Commande validée par « Confirmer la commande ». |
| | Contrôle utilisateur | Retour possible à chaque étape (« Continuer les achats », « Retour au panier », « Retour à la boutique »). |
| **4. Adaptabilité** | Flexibilité | 4 langues (français, anglais, wolof, espagnol), proposées dans l'en-tête sous forme de drapeau et de code (FR, EN, WO, ES). Le nom complet s'affiche au survol et est lu par les lecteurs d'écran. Mise en page adaptée aux petits écrans. |
| | Expérience de l'utilisateur | Lien d'évitement « Aller au contenu principal » pour le clavier. Focus visible. Libellés pour les lecteurs d'écran (`aria-label`, `sr-only`). |
| **5. Gestion des erreurs** | Protection | Confirmation avant de retirer un produit. Bouton d'ajout désactivé quand le stock est atteint. Formulaire inaccessible si le panier est vide. |
| | Qualité des messages | Messages traduits, placés sous le champ concerné (« Veuillez renseigner un nom »), validés dès la saisie (côté client) et côté serveur. |
| | Correction | Les champs en erreur sont encadrés en rouge et conservent les valeurs déjà saisies. |
| **6. Homogénéité / cohérence** | — | Une seule couleur pour les actions principales (vert), un style secondaire (contour) pour les retours, rouge pour la suppression. Même format de prix partout. Même structure de page. |
| **7. Signifiance des codes et dénominations** | — | Icônes toujours accompagnées d'un texte (panier, poubelle, coche), drapeaux accompagnés du code de la langue. Colonne « Action » nommée dans la liste des produits. Libellés clairs : « Adresse » au lieu de « Localisation », « Code postal (facultatif) », « Stock maximum atteint ». Pastilles vertes et orange pour la disponibilité. |
| **8. Compatibilité** | — | Prix en **FCFA**, pays par défaut **Sénégal**, interface en **wolof**, exemples de saisie locaux (Awa Diop, Dakar). |

Fichiers modifiés :
- `Views/Shared/_Layout.cshtml`, `Views/Shared/_Steps.cshtml` (nouveau) ;
- vues `Product`, `Cart`, `Order`, `CartSummary`, `LanguageSelector` ;
- `wwwroot/css/site.css` ;
- `CartController` : message de confirmation, bouton « − », retrait fiable d'un produit ;
- `OrderController` : pays par défaut ;
- fichiers `.resx` en / fr / wo / es.

Les lignes des points d'arrêt de la section 7 sont inchangées.

