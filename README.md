# Kakariko Raid

Juego 3D de **hordas infinitas** inspirado en *The Legend of Zelda: A Link to the Past*. La aldea Kakariko está bajo ataque y Link tiene que defenderla de oleadas de esqueletos, dragonas y un Goblin gigante que aparece como jefe.

Proyecto escolar para la materia **TC2008B**, hecho en **Unity 6** con estilo voxel.

---

## Cómo se juega

- Cada ronda aparecen más enemigos. Cuando mueren todos, empieza la siguiente.
- Cada **10 rondas** aparece el **Goblin** (jefe). Al vencer la ronda 10, **ganas**, y puedes seguir jugando.
- Link tiene **20 corazones** y pierde cuando llegan a 0.
- Al terminar las rondas 3, 6, 9… Link se cura. Los enemigos pueden soltar **piezas de corazón**.

| Enemigo | Comportamiento |
|---|---|
| **Esqueleto** | Te persigue y te golpea (½ corazón). |
| **Dragona** | Vuela a tu alrededor y lanza bolas de fuego (2 corazones). |
| **Goblin (jefe)** | Lento y con mucha vida. De cerca golpea (1.5) y de lejos lanza rocas (3). |

### Controles

| Tecla | Acción |
|---|---|
| **W A S D** | Moverse |
| **Mouse** | Mover la cámara / apuntar |
| **Click izquierdo** | Espadazo / disparar flecha |
| **Mantener click izquierdo y soltar** | Ataque circular cargado (1 s → 1 de daño, 2 s → 4 de daño) |
| **Click derecho (mantener)** | Escudo: bloquea golpes de frente; se rompe tras 5 golpes |
| **Shift** | Rodar (invencible mientras rueda) |
| **Space** | Saltar |
| **F** | Cambiar entre espada y arco |
| **Esc** | Pausa |

---

## Instalación en tu PC

### 1. Requisitos

- **Unity Hub** ([descargar](https://unity.com/download)).
- **Unity 6000.3.28f1**. Instálalo desde Unity Hub → *Installs* → *Install Editor*. Si no aparece en la lista, búscalo en el [archivo de versiones de Unity](https://unity.com/releases/editor/archive).
- **Git** (opcional, para clonar).
- Unos **3 GB libres**. Unity genera su caché la primera vez que abre el proyecto.

Funciona en **Windows, macOS y Linux**.

### 2. Descargar el proyecto

Con Git:

```bash
git clone git@github.com:shegsito/KakarikoRaid-Videojuego.git
```

Si no tienes llave SSH configurada en GitHub, usa la dirección HTTPS:

```bash
git clone https://github.com/shegsito/KakarikoRaid-Videojuego.git
```

Sin Git: en GitHub, botón **Code → Download ZIP**, y descomprímelo.

### 3. Abrirlo en Unity

1. Abre **Unity Hub** → **Projects** → **Add** → **Add project from disk**.
2. Selecciona la carpeta del proyecto (la que contiene `Assets/`, `Packages/` y `ProjectSettings/`).
3. Si Unity Hub avisa que falta la versión **6000.3.28f1**, instálala desde ese mismo aviso.
4. Ábrelo. **La primera vez tarda varios minutos**: Unity descarga los paquetes e importa los modelos. El mapa es pesado.

Los paquetes que usa el proyecto se descargan solos desde `Packages/manifest.json`:
- Universal Render Pipeline 17.3
- glTFast 6.21, para importar los modelos `.glb`
- AI Navigation 2.0, para que los enemigos encuentren el camino
- Input System 1.20

### 4. Jugar

1. En la ventana **Project**, abre `Assets/Scenes/MainMenu`.
2. Presiona **Play ▶** y luego **START**.

> Puedes abrir `Assets/Scenes/Game` y darle Play directamente para probar sin pasar por el menú.

### 5. Crear un ejecutable (opcional)

1. **File → Build Profiles**.
2. Revisa que en *Scene List* estén **MainMenu** (primera) y **Game**.
3. Elige tu plataforma (Windows / macOS / Linux) y presiona **Build**.

---

## Créditos

- **Diseño y desarrollo:** *(agregar nombres del equipo)*
- **Modelos 3D:** Sketchfab. *(Agregar autor y licencia de cada modelo; los que son CC-BY requieren mencionar al autor.)*
- **Efectos de sonido:** generados por código en estilo retro; voces de Link y música agregadas por el equipo.
- Inspirado en *The Legend of Zelda: A Link to the Past* © Nintendo. Proyecto escolar sin fines de lucro.
