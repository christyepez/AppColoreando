# Local Docker Environment

Validated project name: `appcoloreando-dev`.

Public images are pulled from Docker Hub: PostgreSQL 17, Redis 7, RabbitMQ 4 Management, MinIO, Node 22, Nginx 1.27, Prometheus and Grafana. API, Worker and Visual Processor are built from the current repository source.

Local secrets are loaded from `.env`; `.env` is git-ignored and must never be committed. `content-init` prepares the shared `content_data` volume for non-root UID 1654 before content services start.

Validated endpoints:
- API health: `http://localhost:8080/health`
- Admin: `http://localhost:4200`
- Flutter Web: `http://localhost:8083`
- Visual Processor: `http://localhost:8090/health`
- RabbitMQ Management: `http://localhost:15672`
- MinIO Console: `http://localhost:9001`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

Start the stack with `docker compose -p appcoloreando-dev up -d --build`; add `--profile web` when the Flutter Web build should be served on port 8083.


## Local port isolation (trabajo / MarketingIndo)

AppColoreando uses dedicated host ports to avoid collisions with the other local Docker projects:

- Web app: http://127.0.0.1:4210
- Admin: http://127.0.0.1:4211
- API: http://127.0.0.1:8086
- Android emulator API: http://10.0.2.2:8086
- Visual processor: http://127.0.0.1:8090
- PostgreSQL: 127.0.0.1:5432
- RabbitMQ: 127.0.0.1:5672 / management 15672
- MinIO: 127.0.0.1:9000 / console 9001

Do not reassign AppColoreando to host ports 8080, 4200 or 4208 on the trabajo machine; those ports are used by other projects. The Angular admin keeps node_modules in a Docker named volume so npm installation does not run against the Windows bind-mounted node_modules tree.
