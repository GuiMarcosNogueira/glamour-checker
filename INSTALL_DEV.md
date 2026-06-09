# Como Instalar o Glamour Checker (Modo Desenvolvedor)

Para testar o **Glamour Checker** antes de ser aprovado e publicado oficialmente na loja do Dalamud, você precisará carregar o plugin manualmente através das Ferramentas de Desenvolvedor (Developer Tools).

Siga este passo a passo simples:

## Passo 1: Extração
1. Baixe o arquivo `GlamourChecker_Beta.zip`.
2. Extraia o conteúdo do zip para uma pasta no seu computador que você não vá apagar. (Por exemplo: `Documentos\Plugins FFXIV\GlamourChecker`).

## Passo 2: Carregando o Plugin no Dalamud
1. Abra o jogo utilizando o **XIVLauncher**.
2. Uma vez logado no FFXIV, digite `/xlsettings` no chat para abrir as configurações do Dalamud.
3. No menu superior, clique na aba **"Experimental"**.
4. Procure pela opção **"Enable Developer Mode"** (Permite habilitar opções de desenvolvedor) e marque a caixa.
5. Logo abaixo, você verá a seção **"Dev Plugin Locations"**. 
6. Clique no botão **"Select Dev Plugin DLL"** e procure pelo arquivo principal do plugin extraído (que se chama `GlamourChecker.dll`). Ou apenas copie e cole o caminho completo até esse arquivo `.dll` em uma das linhas vazias.
7. Certifique-se de que a caixa **"Enabled"** (Habilitado) está marcada na linha que você acabou de adicionar.

## Passo 3: Ativando o Plugin
1. Digite `/xlplugins` no chat do jogo para abrir o **Plugin Installer**.
2. No menu do lado esquerdo, procure pela aba **"Dev Tools"** e clique em **"Installed Dev Plugins"**.
3. Na lista, você deve encontrar o **"GlamourChecker (dev plugin)"**.
4. Ative o plugin clicando na "chavinha" (switch) do lado inferior esquerdo do nome do plugin para que ela fique verde.

> [!TIP]
> Se a mensagem verde **"No validation issues found in this plugin!"** aparecer, está tudo certo!
> Basta digitar o comando `/glamourchecker` no chat do jogo para abrir a interface!

## Como Atualizar?
Se você receber um novo ZIP para testar:
1. Extraia o novo ZIP na mesma pasta, substituindo todos os arquivos antigos.
2. Com o jogo aberto, volte ao **Plugin Installer** (`/xlplugins`) > **Installed Dev Plugins**.
3. No painel do Glamour Checker, basta clicar no ícone circular de Recarregar (♻️ - botão verde ao lado da chave de ativação) para que a nova versão seja carregada na hora.
