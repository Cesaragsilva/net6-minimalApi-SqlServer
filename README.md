# net6-minimalApi-SqlServer
Artigo de estudo: https://www.youtube.com/watch?v=aXayqUfSNvw


#Como Rodar
1 - Dentro da pasta do projeto, execute o comando abaixo para subir a instância de sqlserver

    docker-compose up -d

2 - Execute as migrations via PackageConsoleManager

    update-database
    update-database -Context NetDevPackAppDbContext
