# Mapa do projeto Nyx

Este guia serve para a equipe encontrar cada parte do protótipo e entender o papel dos scripts. Ele acompanha o jeito que estamos aprendendo C#: uma classe por arquivo, nomes claros e explicações curtas perto das partes principais.

## Pastas

| Pasta | O que fica aqui |
| --- | --- |
| `Art/Claude` | Sprites recebidos para o projeto, separados entre personagem e cenário. |
| `Art/Personagens` | Cientistas e personagens de fundo importados do pacote novo. |
| `Art/Guardas` | Variações de guarda e quadros de animação. |
| `Art/Armadilhas` | Sprites de laser, espinhos, ácido, vapor, esmagador e dispositivos. |
| `Art/Inventario` | Inventário de armas, contador de munição, recarga e caixas verdes. |
| `Art/Generated/Fire` | Quadros de animação do fogo criados para a fase. |
| `Scenes` | Fases e versões anteriores da cena. |
| `Scripts/Personagem` | Movimento do jogador e animações do personagem. |
| `Scripts/Inimigos` | Patrulha dos guardas e projéteis dos guardas armados. |
| `Scripts/Portais` | Superfícies válidas, portais e projétil da arma. |
| `Scripts/Fase` | Regras da fase, tutorial, câmera, objetivos e travessia pelos portais. |
| `Scripts/Interacao` | Caixas empurráveis e caixas de munição que podem ser recolhidas. |
| `Scripts/Perigos` | Fogo e outros perigos da fase. |
| `Scripts/Animacao` | Animações simples que repetem uma sequência de sprites. |
| `Editor` | Ferramentas usadas dentro da Unity para abrir, preparar e conferir a fase. |

## Scripts principais

- `Prisoner.cs`: lê os controles do jogador, move o corpo, faz o pulo e verifica se ele está no chão.
- `PrisonerSpriteAnimator.cs`: escolhe o desenho certo para parado, andando, pulando e atirando.
- `InventarioJogador.cs`: controla carregador, reserva, recarga e os desenhos do inventário na tela.
- `CaixaMunicao.cs`: anima a caixa verde e entrega munição automaticamente ao encostar no jogador.
- `MaintenanceLevel.cs`: junta as regras da fase, como o tutorial, a célula de energia, a saída e a passagem pelos portais.
- `PortalSurface.cs`: define onde um portal pode ser colocado e como a parede se comporta com ele.
- `PortalPad.cs`: representa cada entrada de portal e detecta quando o jogador a atravessa.
- `PortalBolt.cs`: move o disparo até a superfície selecionada.
- `NyxCrate.cs`: prepara a física das caixas empurráveis.
- `GuardaSeguranca.cs`: faz cada guarda patrulhar e decidir quando atirar.
- `ProjetilGuarda.cs`: move o disparo do guarda e verifica se ele atingiu o jogador ou uma parede.
- `NyxCheckpoint.cs`: salva um ponto seguro em cada setor.
- `NyxFireHazard.cs` e `NyxSpikeHazard.cs`: cuidam dos perigos que fazem o jogador voltar ao ponto seguro.
- `NyxTimedTrap.cs` e `NyxCrusherTrap.cs`: controlam armadilhas com aviso e intervalo para passar.
- `NyxDamageZone.cs`: cuida das regiões fatais sob os vãos.
- `SpriteLoop.cs`: alterna sprites em sequência, por exemplo em um alarme ou ventilador.

## Palavras de C# que aparecem aqui

- `Awake` e `Start` preparam um objeto quando ele entra na cena.
- `Update` é usado para ler controles e atualizar elementos visuais.
- `FixedUpdate` é usado para movimento e física.
- Campos `public` aparecem no Inspector da Unity para a equipe configurar objetos e sprites.
- Uma `Coroutine` pausa entre quadros. A fase usa isso para animar a entrada e a saída do portal.

Os scripts já ligados à cena mantêm os nomes de classe existentes, porque a Unity guarda essas referências. Para scripts novos, escolhemos um nome que explique uma tarefa de cada vez e mantemos o arquivo com o mesmo nome da classe. Não precisamos adicionar sistemas ou padrões avançados quando uma função simples resolve.

## Abrir e jogar

Abra o projeto com a versão da Unity indicada em `ProjectSettings/ProjectVersion.txt`. A fase principal é `Scenes/Manutencao_Interativa.unity`.

Ao concluir a manutenção, a porta leva para `Scenes/Fuga_02.unity`. Nessa cena, a pistola é apresentada antes dos cinco trechos de inimigos e armadilhas. O tutorial explica como atirar, recarregar e recolher munição. Cada sala apresenta um perigo por vez, com cobertura, placas de portal e pontos de retorno.

- **A/D** ou **setas**: andar e empurrar caixas.
- **Espaço**: pular. Segurar deixa o salto mais alto.
- **Mouse**: mirar. Com o lançador, esquerdo abre o portal roxo e direito abre o verde; com a pistola, esquerdo atira.
- **E**: pegar a pistola, pegar/soltar/instalar a célula e interagir com a porta. A munição é recolhida automaticamente ao encostar na caixa verde.
- **Q**: trocar entre a pistola e os portais.
- **R**: recarregar a pistola. São 12 tiros no carregador, 36 de reserva inicial e mais 12 por caixa verde.
- **F5**: reiniciar a fase.
