\# Job Queue System



A backend job queue API built with \*\*ASP.NET Core (.NET 10)\*\*, PostgreSQL, RabbitMQ, and background processing. The system provides endpoints for creating, retrieving, and updating jobs while supporting observability and health monitoring.



\## Live Demo



\- \*\*API Base URL:\*\* https://jobqueue-api.onrender.com

\- \*\*Swagger UI:\*\* https://jobqueue-api.onrender.com/swagger

\- \*\*Health Check:\*\* https://jobqueue-api.onrender.com/health

\- \*\*Database Readiness:\*\* https://jobqueue-api.onrender.com/health/ready

\- \*\*Metrics:\*\* https://jobqueue-api.onrender.com/metrics



> The API is hosted on Render, and PostgreSQL is hosted on Neon. The free hosting instance may take some time to respond after periods of inactivity.



\## Features



\- Create and retrieve jobs through REST API endpoints.

\- Retrieve individual jobs by ID.

\- Update job statuses.

\- Persist job data using PostgreSQL.

\- Integrate RabbitMQ for messaging.

\- Run background job processing using a hosted worker.

\- Track job attempts, errors, timestamps, and processing information.

\- Use GUID-based row versions for concurrency tracking.

\- Monitor application health and database connectivity.

\- Expose application metrics through OpenTelemetry and Prometheus.

\- Use structured logging with Serilog.

\- Run automated tests and Docker builds through GitHub Actions.



\## Tech Stack



| Technology | Purpose |

|---|---|

| C# | Primary programming language |

| ASP.NET Core / .NET 10 | REST API |

| Entity Framework Core | Database access and migrations |

| PostgreSQL | Persistent data storage |

| Neon | Managed PostgreSQL hosting |

| RabbitMQ | Messaging |

| Docker | Containerization |

| OpenTelemetry | Metrics and observability |

| Prometheus | Metrics scraping |

| Serilog | Structured logging |

| Swagger / OpenAPI | API documentation and testing |

| xUnit | Automated testing |

| GitHub Actions | Continuous integration |

| Render | API deployment |



\## API Endpoints



Base URL:



`https://jobqueue-api.onrender.com`



| Method | Endpoint | Description |

|---|---|---|

| `POST` | `/api/Jobs` | Create a job |

| `GET` | `/api/Jobs` | Retrieve jobs |

| `GET` | `/api/Jobs/{id}` | Retrieve a job by ID |

| `PUT` | `/api/Jobs/{id}/status` | Update a job's status |

| `GET` | `/health` | Check API health |

| `GET` | `/health/ready` | Check database connectivity |

| `GET` | `/metrics` | Expose application metrics |



For interactive API documentation, visit the \[Swagger UI](https://jobqueue-api.onrender.com/swagger).



\## Getting Started



\### Prerequisites



Install the following:



\- \[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

\- \[Git](https://git-scm.com/)

\- \[Docker Desktop](https://www.docker.com/products/docker-desktop/)

\- A PostgreSQL database

\- A RabbitMQ instance



\### 1. Clone the repository



```bash

git clone https://github.com/geep97/JOB-QUEUE-SYSTEM.git

cd JOB-QUEUE-SYSTEM

```



\### 2. Configure environment variables



Configure the following application settings before running the API:



| Environment variable | Description |

|---|---|

| `ConnectionStrings\_\_DefaultConnection` | PostgreSQL connection string |

| `RabbitMq\_\_Url` | RabbitMQ connection URL |



For local development, use .NET User Secrets to store sensitive values:



```bash

dotnet user-secrets init --project JobQueue/JobQueue.csproj

```



Set the PostgreSQL connection string:



```bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR\_HOST;Database=YOUR\_DATABASE;Username=YOUR\_USERNAME;Password=YOUR\_PASSWORD;SSL Mode=Require" --project JobQueue/JobQueue.csproj

```



Set the RabbitMQ URL:



```bash

dotnet user-secrets set "RabbitMq:Url" "YOUR\_RABBITMQ\_URL" --project JobQueue/JobQueue.csproj

```



Replace the placeholders with your own configuration. Never commit passwords, connection strings, or other secrets to Git.



\### 3. Apply database migrations



If the EF Core CLI is installed, run:



```bash

dotnet ef database update --project JobQueue/JobQueue.csproj

```



Ensure the configured PostgreSQL database is reachable before applying migrations.



\### 4. Run the application



```bash

dotnet restore JobQueue/JobQueue.csproj

dotnet run --project JobQueue/JobQueue.csproj

```



The API will display its local address in the terminal. Swagger and the OpenAPI document are available in the Development environment.



\### 5. Run the tests



```bash

dotnet test JobQueue/JobQueue.Tests/JobQueue.Tests.csproj --configuration Release

```



\### 6. Build the Docker image



Run this command from the repository root:



```bash

docker build -t jobqueue-api -f JobQueue/Dockerfile .

```



Run the container with your environment variables configured:



```bash

docker run -p 8080:8080 \\

&#x20; -e "ConnectionStrings\_\_DefaultConnection=YOUR\_POSTGRES\_CONNECTION\_STRING" \\

&#x20; -e "RabbitMq\_\_Url=YOUR\_RABBITMQ\_URL" \\

&#x20; jobqueue-api

```



Replace the placeholder values before running the container.



\## Example API Usage



\### Create a job



\*\*Request\*\*



```http

POST /api/Jobs

Content-Type: application/json

```



```json

{

&#x20; "type": "NormalJob",

&#x20; "payload": "Email"

}

```



\*\*Example response — `201 Created`\*\*



```json

{

&#x20; "id": 1,

&#x20; "type": "NormalJob",

&#x20; "payload": "Email",

&#x20; "status": 0,

&#x20; "attempts": 0,

&#x20; "errors": null,

&#x20; "createdAt": "2026-10-24T15:19:56.6709014Z",

&#x20; "claimedAt": null,

&#x20; "processAfter": "2026-10-24T15:19:56.6709222Z",

&#x20; "rowVersion": "c74bdf9c-8f77-4785-b146-00a1a2d77464"

}

```



The values above are an example of the API response. Generated IDs, timestamps, and row versions will vary.



\### Retrieve a job



```http

GET /api/Jobs/1

```



\### Retrieve all jobs



```http

GET /api/Jobs

```



\### Update job status



```http

PUT /api/Jobs/1/status

```



Use Swagger to inspect the required request body and supported status values.



\## Health Monitoring



The application exposes health and observability endpoints:



\- `/health` — checks application health checks.

\- `/health/ready` — verifies database connectivity.

\- `/metrics` — exposes Prometheus-compatible metrics.



These endpoints help with monitoring the running service and diagnosing connectivity issues.



\## Continuous Integration



GitHub Actions runs automated checks when changes are pushed or pull requests are opened. The workflow restores dependencies, builds the API, runs tests, and builds the Docker image.



\## Deployment



The API is deployed to Render using Docker. PostgreSQL is hosted on Neon, while RabbitMQ is configured through an environment variable.



Deployment configuration and credentials should be managed through environment variables rather than committed to the repository.



\## Future Improvements



\- Add authentication and authorization.

\- Implement retry policies and dead-letter handling.

\- Add more integration tests for database and message processing.

\- Improve structured error reporting and operational monitoring.

\- Add pagination and filtering for job retrieval.

\- Protect production API documentation where appropriate.



\## Author



\*\*Godman Owusu Obeng-Denteh\*\*



\- \[GitHub](https://github.com/geep97)

\- \[LinkedIn](https://www.linkedin.com/in/godman-od/)





