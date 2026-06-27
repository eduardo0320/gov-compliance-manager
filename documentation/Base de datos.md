## borra la base de datos vieja actual

dotnet ef database drop --force

## limpia los archivos en caché o corruptos

dotnet clean

# MIGRACIONES

## Crear migracion:

1. dotnet ef migrations add <NombreDeLaMigracion>
2. REVISAR LOS DATOS DE LA MIGRACION (varibles correctas) Y CONFIGURARLA UP Y DOWN (preguntarle a chat)
3. al correr dotnet run se aplica automaticamente

## Eliminar migracion:

1. dotnet ef migrations list -> listar las migraciones existentes
2. dotnet ef database update <nombre_migracion_anterior> -> vuelve a la migracion que ingrese
3. borrar ambas migraciones manualmente (la normal y el .designer)
