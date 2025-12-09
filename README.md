# ContosoProtect API 🛡️

[![.NET Build and Test](https://github.com/frkim/ContosoProtect/actions/workflows/dotnet.yml/badge.svg)](https://github.com/frkim/ContosoProtect/actions/workflows/dotnet.yml)

API de démonstration pour **ContosoProtect**, inspirée des activités d'IRCEM en **protection sociale** (retraite, prévoyance, assurance santé complémentaire), destinée aux **emplois de la famille** et aux **services à la personne**.

Cette application démontre comment **GitHub Copilot Coding Agent** peut générer rapidement une **Minimal API .NET** complète avec calcul de devis, validation, documentation Swagger, tests automatisés et base de données en mémoire.

## 🎯 Fonctionnalités

- ✅ **Calcul de devis** avec règles métier explicables
- ✅ **Validation des entrées** avec messages d'erreur détaillés
- ✅ **Documentation Swagger/OpenAPI** interactive
- ✅ **Base de données en mémoire** réinitialisable
- ✅ **Liste et recherche de devis** avec pagination
- ✅ **Statistiques** sur les devis calculés
- ✅ **Support CORS** pour intégration frontend
- ✅ **Tests unitaires et d'intégration** (24 tests)
- ✅ **CI/CD avec GitHub Actions**

## 🏗️ Architecture

```
ContosoProtect/
├── ContosoProtect.Api/          # API principale
│   ├── Models/                  # Modèles de domaine
│   ├── Services/                # Logique métier
│   ├── Data/                    # Contexte EF Core
│   └── Program.cs               # Configuration et endpoints
└── ContosoProtect.Api.Tests/    # Tests xUnit
```

## 📋 Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Un éditeur de code (Visual Studio, VS Code, Rider)

## 🚀 Démarrage rapide

### 1. Cloner le dépôt

```bash
git clone https://github.com/frkim/ContosoProtect.git
cd ContosoProtect
```

### 2. Restaurer les dépendances

```bash
dotnet restore
```

### 3. Lancer l'application

```bash
cd ContosoProtect.Api
dotnet run
```

L'API sera accessible sur :
- **Swagger UI** : http://localhost:5083 (ou le port indiqué dans la console)
- **API** : http://localhost:5083/quote

### 4. Exécuter les tests

```bash
dotnet test
```

## 📚 API Endpoints

### `POST /quote` - Calculer un devis

Calcule une prime d'assurance prévoyance avec décomposition des règles appliquées.

**Exemple de requête :**

```bash
curl -X POST http://localhost:5083/quote \
  -H "Content-Type: application/json" \
  -d '{
    "age": 42,
    "status": "SalariedEmployee",
    "familyOption": true,
    "accidentOption": false,
    "seniorityMonths": 36
  }'
```

**Réponse (200 OK) :**

```json
{
  "premium": 17.00,
  "breakdown": [
    {
      "code": "BASE",
      "description": "Base premium",
      "amount": 12.00
    },
    {
      "code": "AGE_31_50",
      "description": "Age bracket 31-50",
      "amount": 3.00
    },
    {
      "code": "STATUS_EMPLOYEE",
      "description": "Salaried employee discount",
      "amount": -1.00
    },
    {
      "code": "FAMILY_OPTION",
      "description": "Family coverage option",
      "amount": 4.00
    },
    {
      "code": "SENIORITY_DISCOUNT",
      "description": "Loyalty discount (24+ months)",
      "amount": -1.00
    }
  ]
}
```

**Erreur de validation (400 Bad Request) :**

```json
{
  "error": "validation_error",
  "details": [
    {
      "field": "age",
      "message": "Age must be between 18 and 75."
    }
  ]
}
```

### `GET /health` - Vérifier l'état de l'API

```bash
curl http://localhost:5083/health
```

**Réponse :**

```json
{
  "status": "healthy",
  "timestamp": "2025-12-09T10:30:00.000Z"
}
```

### `POST /admin/reset` - Réinitialiser la base de données

**⚠️ Nécessite un token d'administration**

```bash
curl -X POST http://localhost:5083/admin/reset \
  -H "X-Admin-Token: demo-reset"
```

**Réponse :**

```json
{
  "message": "Database reset successfully",
  "timestamp": "2025-12-09T10:30:00.000Z"
}
```

### `GET /quotes` - Liste des devis avec pagination

Récupère tous les devis enregistrés avec support de pagination.

**Paramètres de requête :**
- `page` (optionnel) : Numéro de page (défaut: 1)
- `pageSize` (optionnel) : Nombre d'éléments par page (défaut: 10, max: 100)

```bash
curl http://localhost:5083/quotes?page=1&pageSize=10
```

**Réponse :**

```json
{
  "page": 1,
  "pageSize": 10,
  "totalQuotes": 2,
  "totalPages": 1,
  "quotes": [
    {
      "id": 2,
      "age": 50,
      "status": 1,
      "familyOption": false,
      "accidentOption": true,
      "seniorityMonths": 24,
      "premium": 19.00,
      "createdAt": "2025-12-09T11:31:51.116Z"
    },
    {
      "id": 1,
      "age": 42,
      "status": 0,
      "familyOption": true,
      "accidentOption": false,
      "seniorityMonths": 36,
      "premium": 17.00,
      "createdAt": "2025-12-09T11:31:42.087Z"
    }
  ]
}
```

### `GET /quotes/{id}` - Récupérer un devis spécifique

```bash
curl http://localhost:5083/quotes/1
```

**Réponse (200 OK) :**

```json
{
  "id": 1,
  "age": 42,
  "status": 0,
  "familyOption": true,
  "accidentOption": false,
  "seniorityMonths": 36,
  "premium": 17.00,
  "createdAt": "2025-12-09T11:31:42.087Z"
}
```

**Erreur (404 Not Found) :**

```json
{
  "error": "Quote not found",
  "id": 999
}
```

### `GET /statistics` - Statistiques sur les devis

Récupère des statistiques agrégées sur tous les devis.

```bash
curl http://localhost:5083/statistics
```

**Réponse :**

```json
{
  "totalQuotes": 2,
  "averagePremium": 18.00,
  "minPremium": 17.00,
  "maxPremium": 19.00,
  "byStatus": {
    "SalariedEmployee": 1,
    "HouseholdEmployer": 1
  },
  "withFamilyOption": 1,
  "withAccidentOption": 1
}
```

## 💼 Règles métier

Le calcul de la prime suit ces règles :

### Base
- **Prime de base** : 12.00 € / mois

### Âge
- **18-30 ans** : +0 €
- **31-50 ans** : +3 €
- **51-65 ans** : +6 €
- **66-75 ans** : +9 €
- **> 75 ans** : Refus (validation error)

### Statut
- **Salarié** (`SalariedEmployee`) : -1 €
- **Employeur particulier** (`HouseholdEmployer`) : +2 €

### Options
- **Option famille** (`FamilyOption`) : +4 €
- **Option accident** (`AccidentOption`) : +3 €

### Ancienneté
- **≥ 24 mois** : -1 € (remise fidélité)

### Exemple de calcul

Pour un salarié de 42 ans avec option famille et 36 mois d'ancienneté :
- Base : 12 €
- Âge 31-50 : +3 €
- Statut employé : -1 €
- Option famille : +4 €
- Ancienneté : -1 €
- **Total : 17 €**

## 🧪 Tests

Le projet contient **24 tests** couvrant :

### Tests unitaires (`QuoteServiceTests`)
- ✅ Calcul de prime avec différentes combinaisons
- ✅ Application correcte de chaque règle métier
- ✅ Validation des entrées (âge, ancienneté)
- ✅ Gestion des cas limites

### Tests d'intégration (`QuoteApiTests`)
- ✅ Endpoints HTTP (POST /quote, GET /health, POST /admin/reset)
- ✅ Nouveaux endpoints (GET /quotes, GET /quotes/{id}, GET /statistics)
- ✅ Pagination des résultats
- ✅ Validation des erreurs 400 et 404
- ✅ Authentification admin
- ✅ Scénarios complexes

```bash
# Exécuter tous les tests
dotnet test

# Avec détails
dotnet test --logger "console;verbosity=detailed"

# Avec couverture de code
dotnet test --collect:"XPlat Code Coverage"
```

## 🛠️ Technologies utilisées

- **.NET 10** - Framework principal
- **ASP.NET Core Minimal API** - API REST
- **Entity Framework Core In-Memory** - Base de données
- **Swashbuckle (Swagger)** - Documentation OpenAPI
- **xUnit** - Framework de tests
- **Microsoft.AspNetCore.Mvc.Testing** - Tests d'intégration

## 📦 Structure des données

### QuoteRequest

```csharp
{
  "age": int,                      // 18-75
  "status": string,                // "SalariedEmployee" | "HouseholdEmployer"
  "familyOption": bool,            // Option famille
  "accidentOption": bool,          // Option accident
  "seniorityMonths": int           // Mois d'ancienneté (≥0)
}
```

### QuoteResponse

```csharp
{
  "premium": decimal,              // Prime totale
  "breakdown": [                   // Détail des règles
    {
      "code": string,              // Code de la règle
      "description": string,       // Description
      "amount": decimal            // Montant (+ ou -)
    }
  ]
}
```

## 🔒 Sécurité

- ✅ Validation stricte des entrées
- ✅ Authentification par token pour l'endpoint admin
- ✅ Base de données en mémoire (pas de données sensibles persistées)
- ✅ HTTPS configuré en production
- ⚠️ **Note** : Dans un environnement de production, utilisez un système d'authentification robuste (OAuth2, JWT, etc.)

## 🤝 Contribution

Ce projet est une démonstration. Pour contribuer :

1. Fork le projet
2. Créez une branche feature (`git checkout -b feature/AmazingFeature`)
3. Committez vos changements (`git commit -m 'Add AmazingFeature'`)
4. Push vers la branche (`git push origin feature/AmazingFeature`)
5. Ouvrez une Pull Request

## 📄 Licence

Voir le fichier [LICENSE](LICENSE) pour plus de détails.

## 🙏 Remerciements

- Inspiré par les activités d'**IRCEM** en protection sociale
- Créé avec **GitHub Copilot Coding Agent**
- Framework **ASP.NET Core** de Microsoft

---

**Note** : Cette application est une démonstration pédagogique. Elle ne doit pas être utilisée en production sans adaptations appropriées pour la sécurité, la scalabilité et la conformité réglementaire.
