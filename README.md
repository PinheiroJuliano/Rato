# Ratinho Desktop 🐭

Um mascote de desktop interativo e divertido construído em C# e WPF (.NET 10). O aplicativo exibe o famoso meme do ratinho girando flutuando sobre a sua tela com fundo transparente, suporte a arrasto por clique, modo automático "DVD Screensaver" e efeitos sonoros gerados por código (chiptune).

---

## 🚀 Funcionalidades

- **Mascote Flutuante**: Janela sem bordas e fundo transparente.
- **Movimentação por Clique**: Arrasta o Ratinho para qualquer canto da sua tela segurando-o com o botão esquerdo.
- **Modo DVD (Quicar)**: Com um clique duplo com o botão esquerdo, o Ratinho começa a quicar pelas bordas da tela. O Ratinho se inverte horizontalmente de acordo com a direção do movimento!
- **Áudio Sintetizado Localmente**: Efeitos sonoros de clique e música retro de fundo criados programaticamente em tempo de compilação (sem necessidade de conexões externas).
- **Menu de Contexto Completo (Botão Direito)**:
  - **Tamanho**: Altera o tamanho entre Pequeno (100px), Médio (200px) e Grande (320px).
  - **Opacidade**: Controla a transparência da janela (100%, 75%, 50%, 25%).
  - **Sons e Música**: Configura separadamente o guincho de clique ou a música retro em loop.
  - **Sempre no Topo**: Fixa o ratinho acima de outras janelas.
  - **Sair**: Fecha o aplicativo.

---

## 🎹 Atalho Global de Teclado

O projeto inclui um script que configura um atalho global no Windows:
- Teclas: **`Ctrl + Alt + R`**

Para abrir o Ratinho a qualquer momento do seu Windows, basta pressionar essa combinação de teclas.

---

## 🛠️ Como Executar em Modo de Desenvolvimento

Certifique-se de ter o SDK do .NET 10.0 instalado.

1. Navegue até a pasta do projeto:
   ```bash
   cd RatinhoDesktop
   ```
2. Restaure e execute o projeto:
   ```bash
   dotnet run
   ```

---

## 📦 Como Gerar o Executável (.exe) de Produção

Para compilar o aplicativo como um arquivo executável único (Single File) leve e sem dependências extras, execute o seguinte comando dentro da pasta `RatinhoDesktop`:

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

O arquivo executável único (`RatinhoDesktop.exe`) será gerado em:
`RatinhoDesktop/bin/Release/net10.0-windows/win-x64/publish/`

---

## 📂 Estrutura do Projeto

```
Rato/
├── README.md                      # Este arquivo
└── RatinhoDesktop/
    ├── RatinhoDesktop.csproj      # Configuração do projeto e dependências NuGet
    ├── App.xaml / App.xaml.cs     # Inicialização da aplicação WPF
    ├── MainWindow.xaml            # Design XAML da janela transparente e menus
    ├── MainWindow.xaml.cs         # Lógica em C# (Modo DVD, áudios e interações)
    ├── SoundGenerator.cs          # Gerador em C# que sintetiza as ondas de áudio (.wav)
    └── Assets/
        └── rato.gif               # GIF do ratinho em rotação (recurso embutido)
```
