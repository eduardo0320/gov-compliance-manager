# COMANDOS GIT PARA MANEJO DEL REPOSITORIO

git status → Muestra el estado del repositorio. (para ver qué falta o se necesita para hacer push)

git branch → Lista las ramas existentes.

git branch <nombre> → Crea una nueva rama.

git checkout <rama> → Cambia a otra rama.

# Pasos para subir cambios de una rama al repositorio

// Elegir uno
1- git add <archivo> → Añade un archivo al área de preparación.
1- git add . → Añade todos los cambios.

2- git commit -m "mensaje" → Guarda los cambios.

3- git push -> Sube los cambios A LA RAMA DEL REPOSITORIO REMOTO

4- git merge <rama> → Fusiona la RAMA especificada con la ACTUAL (usar git checkout para cambiar entre ramas, normalmente hacemos esto para fusionar una rama con el main).

# Pasos para CARGAR ramas del repositorio remoto con sus cambios

1- git fetch --all -> Actualiza las ramas remotas

2- git checkout -b refactor/formateo-de-codigo origin/refactor/formateo-de-codigo -> Crea una rama local a partir de la rama en el repositorio remoto \*(GITHUB)

3- git branch -> ver las ramas locales

# Pasos para ELIMINAR ramas finalizadas

1- git branch -d <rama> -> Eliminar la rama LOCAL \*(Aun sigue en GITHUB la rama)

2- git push origin --delete <rama> -> Elimina la rama remota (GITHUB)

# Otros Comandos

git log → Muestra el historial de commits.

git diff → Ver los cambios en archivos modificados.

git stash → guarda cambios temporales 