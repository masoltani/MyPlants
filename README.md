# MyPlants

Application web de gestion des plantes pour suivre les espèces, les dates d’arrosage et l’historique des soins. Le projet est conçu comme une Progressive Web App (PWA) exécutée côté navigateur avec un stockage local pour un usage rapide, puis une synchronisation optionnelle avec le cloud.

## Description du projet

MyPlants permet de :

- ajouter et organiser des plantes
- consulter les informations de soins et les dates d’arrosage
- enregistrer un historique d’arrosage par plante
- visualiser les arrosages à venir
- personnaliser l’affichage des dates selon la culture ou les préférences de l’utilisateur
- exporter/importer une sauvegarde locale
- se connecter et synchroniser les données avec Supabase
- installer l’application sur un navigateur et la conserver accessible comme application web locale

Le projet est pensé pour un usage personnel ou domestique, avec une interface simple et une forte dépendance au navigateur et au stockage local.

## Stack technique

### Frontend

- C# / .NET 10
- Blazor WebAssembly
- Razor Components
- HTML, CSS et JavaScript
- Bootstrap

### Données et synchronisation

- IndexedDB pour le stockage local des plantes et des arrosages
- localStorage pour les préférences et certains états applicatifs
- Supabase pour l’authentification et la synchronisation cloud

### Outils / infrastructure

- GitHub Actions pour la publication
- GitHub Pages pour le déploiement public de l’instance officielle
- Service worker pour les capacités PWA
- Ressources .resx pour la localisation (français, anglais, arabe)

## Architecture du projet

```text
MyPlants/
├── Components/      # Composants Razor réutilisables
├── Pages/           # Pages de l’application
├── Models/          # Modèles métier
├── Dtos/            # Objets de transfert pour Supabase
├── Services/        # Services applicatifs (sync, prefs, backup, auth, etc.)
├── Repositories/    # Accès aux données locales et distantes
├── Businesses/      # Logique métier et règles de synchronisation
├── Interfaces/      # Contrats d’implémentation
├── Resources/       # Ressources de localisation
├── Layout/          # Mise en page globale
├── wwwroot/         # Assets statiques, JS, CSS, config, PWA
├── Program.cs       # Configuration DI et initialisation de l’application
├── MyPlants.csproj  # Définition du projet
├── App.razor        # Point d’entrée applicatif
├── .github/workflows/deploy.yml
├── LICENSE
└── README.md
```

## Fonctionnement global

### 1. Stockage local

Les opérations courantes de gestion des plantes sont stockées localement dans IndexedDB. Cela permet :

- un accès rapide à l’interface
- un fonctionnement même sans connexion serveur
- la conservation de l’historique d’arrosage
- les sauvegardes et restaurations locales

### 2. Authentification et synchronisation

L’application peut utiliser Supabase pour :

- créer un compte ou se connecter
- gérer une session utilisateur
- synchroniser les plantes et les arrosages entre le navigateur et le cloud

La synchronisation est gérée de façon incrémentale : l’application ne relit pas tout le cloud à chaque fois. Elle garde un curseur de position pour ne récupérer que les changements les plus récents. La synchronisation traite d’abord les plantes, puis les arrosages associés.

### 3. Sauvegarde / restauration

L’application dispose d’un mécanisme de sauvegarde des plantes et de leurs arrosages. Le fichier exporté peut être importé sur la même application pour réhydrater les données, sans écraser celles déjà présentes.

### 4. PWA

Le projet est rendu compatible avec le modèle PWA via :

- service worker
- manifest web
- icônes et assets statiques
- installation depuis le navigateur

## Librairies et dépendances importantes

### NuGet / .NET

- `Microsoft.AspNetCore.Components.WebAssembly`
- `Microsoft.AspNetCore.Components.WebAssembly.DevServer`

### Frontend

- Bootstrap (fichiers statiques dans `wwwroot/lib/bootstrap`)
- Supabase JavaScript SDK (fichiers statiques dans `wwwroot/lib/supabase`)

### Structure de fichiers JS

Le dossier `wwwroot/js` contient de nombreuses fonctions d’interopérabilité navigateur, notamment :

- `indexedDb.js` : accès à IndexedDB
- `localStorage.js` : stockage de préférences
- `supabaseClient.js` : configuration du client Supabase
- `supabasePlants.js` / `supabaseWaterings.js` : gestion des données cloud
- `authLink.js` : gestion des liens d’authentification
- `autoSync.js` : synchronisation automatique
- `files.js` : export/import / gestion de fichiers
- `appUpdate.js` : mise à jour de l’application

## Déploiement

Le workflow GitHub Actions présent dans `.github/workflows/deploy.yml` publie le site sur GitHub Pages lors d’un push sur la branche `master`.

Les étapes principales du workflow :

- installation du SDK .NET
- publication de l’application avec `dotnet publish`
- ajustement du `base href` pour GitHub Pages
- upload de la sortie web comme artifact Pages
- déploiement automatique vers GitHub Pages

## Prérequis pour le développement local

- .NET 10 SDK
- un navigateur moderne
- une instance Supabase configurée si les fonctions de synchronisation cloud sont utilisées

### Lancer le projet localement

```bash
dotnet run --project MyPlants/MyPlants.csproj
```

### Publier la version de production

```bash
dotnet publish MyPlants/MyPlants.csproj -c Release -o release
```

## Sécurité et bonnes pratiques

- Ne jamais committer de secrets ou de clés sensibles dans le dépôt public.
- Les clés de type `publishable` sont visibles côté client et ne doivent pas être confondues avec des secrets serveur.
- Les règles d’accès Supabase (RLS) doivent être conçues pour limiter les données accessibles.
- Les fichiers exportés locaux ne doivent pas contenir des informations sensibles non souhaitées.

## Licence

Ce projet est distribué sous une licence propriétaire. L’usage public et le déploiement officiel sur GitHub Pages sont autorisés uniquement pour l’instance officielle du titulaire des droits. Toute copie, modification, adaptation, fork de développement, hébergement hors de l’instance officielle, déploiement sur un autre service ou autre usage non expressément autorisé est interdit sans autorisation écrite préalable.

Voir le fichier `LICENSE` pour les détails complets.

## Remarques

- Le dépôt est public pour la publication GitHub Pages et la diffusion de l’instance officielle.
- L’objectif est de garder un accès public au site sans autoriser la réutilisation ou le déploiement non autorisé du code.
- Les éléments sensibles, secrets ou clés d’authentification doivent toujours rester hors du dépôt public.


