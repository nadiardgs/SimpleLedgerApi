# Simple Ledger API

This RESTFul API service manages basic financial transactions.

---
### Technologies used
* **Language:** C#
* **Framework:** .NET 9
* **Development Environment:** Rider
* **Libraries/Tools:**
    * [Moq](https://github.com/moq/moq4)
    * [FluentAssertions](https://fluentassertions.com/)
    * [xUnit](https://xunit.net/)
    * [FluentValidation](https://fluentvalidation.net/)

---
### How to Run the Project

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/nadiardgs/SimpleLedgerApi
    ```
2.  **Navigate to the project directory:**
    ```bash
    cd SimpleLedgerApi/SimpleLedgerApi 
    ```
3.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```
4.  **Run the application:**
    ```bash
    dotnet run
    ```
    * **Troubleshooting:** If you see "Couldn't find a project to run", ensure you are in the directory containing the `.csproj` file.
5.  When the console shows "Application started.", the API is ready for testing. It runs on `https://localhost:5186`. Swagger is configured for this service, on `https://localhost:5186/swagger`

---

### Endpoints
#### 1. `GET /api/balances`

* **Description:** Returns the current available balance in the ledger.
* **Request Body:** Empty
* **Example Response Body:**
    ```json
    {
      "balance": 0,
      "date": "2025-07-04T15:21:49.869Z"
    }
    ```

#### 2. `GET /api/transactions`

* **Description:** Lists all transactions registered in the ledger, from the most to the least recent.
* **Request Body:** Empty
* **Example Response Body:**
    ```json
    [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "amount": 0,
        "timestamp": "2025-07-04T15:23:46.657Z",
        "type": "Deposit",
        "description": "string"
      }
    ]
    ```

#### 3. `POST /api/transactions`

* **Description:** Records a new money movement (deposit or withdrawal) to the ledger.
* **Rules (Validation):**
    * Amount must be a valid number greater than 0. (Returns 400 Bad Request)
    * Type must be "deposit" or "withdrawal" (case-insensitive). (Returns 400 Bad Request)
    * Description is required for withdrawals. (Returns 400 Bad Request)
    * Description cannot be longer than 500 characters. (Returns 400 Bad Request)
    * Withdrawals require sufficient funds. (Returns 400 Bad Request)
* **Example Request Body:**
    ```json
    {
      "amount": 400,
      "type": "Deposit",
      "description": "Gift"
    }
    ```
* **Example Response Body (on success - HTTP 201 Created):**
    ```json
    {
      "id": "bdd55601-f7a2-4163-84c4-164f238625b7",
      "amount": 400,
      "timestamp": "2025-07-04T15:26:25.1758414Z",
      "type": "Deposit",
      "description": "Gift"
    }
    ```

---


### Design Decisions

* **Layered Architecture:** The separation into Controller, Service and Model classes allow for better testability, maintainability and scalability.
* **In-Memory Data Store:** The list of transactions is stored in a IEnumerable variable for simplicity. In a real world scenario, the use of Entity Framework and a database for structure data persistency.
* **Dependency Injection:** The service class and its interface were injected as a Singleton to ensure data persistency during runtime.
* **Data validation:** To ensure the rules for a transaction, FluentValidator was used to centralize input validation, allowing more flexibility and testability.
* **Testing strategy:** While it is good practice to test Service, Validation and Controller classes, the unit tests here are limited to the Controller class for simplicity. They cover happy paths, input validation (e.g., negative amounts, missing types, description length), business logic validation (e.g., insufficient funds), and API error handling (400s and 500s).

#### Future Testing Enhancements

In a real-world production environment, the tests would be expanded to include:

* Dedicated **Unit Tests** for the `ILedgerService` (to test business logic in isolation).
* Dedicated **Unit Tests** for the `NewTransactionRequestValidator` (to test validation rules in isolation).
* Integration tests for the data persistence layer.
* End-to-end tests covering the full system flow.
