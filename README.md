# Além do Portal: Fuga de Nýx

Nýx é um jogo 2D de plataforma e quebra-cabeças para PC, em desenvolvimento na Unity para uma game jam. O jogo combina pixel art, exploração de uma prisão lunar e desafios que usam um par de portais para atravessar o cenário.

## Equipe

- Lustosa
- Arthur
- Davi
- Daniel

## História

Num futuro próximo, um prisioneiro é retirado da cela para testar uma arma experimental que abre portais. Durante o teste, um asteroide atinge a Lua perto da prisão e provoca um apagão. Ele aproveita a queda de energia, pega a arma e arranca a tampa de um duto de ventilação para escapar.

## O protótipo

A fase de manutenção começa logo após a saída pelo duto. O jogador aprende a criar dois portais conectados nas superfícies brancas, atravessa um vão em chamas e leva uma célula auxiliar até o terminal para destrancar a saída. Essa fase é a base para os próximos trechos da fuga e para a introdução cinematográfica planejada.

## Como o projeto está organizado

Os arquivos da fase ficam em `Assets/Nyx`. As imagens ficam em `Art`, as cenas em `Scenes` e os scripts são separados por assunto. O guia [Assets/Nyx/README.md](Assets/Nyx/README.md) explica o papel de cada script e algumas partes de C# usadas no protótipo.

A manutenção e a ala de segurança são cenas separadas. Na segunda cena, o jogador encontra uma pistola e passa por cinco salas de treino, cada uma apresentando um inimigo e uma armadilha. Há cientistas ao fundo, caixas para cobertura, rotas de portal e checkpoints entre as salas.

O inventário no canto da tela mostra a ferramenta equipada. A pistola começa com 12 balas no carregador e 36 na reserva, com um intervalo mínimo de 0,4 segundo entre os tiros. Recarregar leva 1,1 segundo; encostar nas caixas verdes recolhe automaticamente mais 12 balas para a reserva, até o limite de 120. Se a reserva estiver cheia, a caixa permanece no chão. Os portais continuam disponíveis sem consumir munição. Ao morrer, a munição e as caixas voltam ao estado salvo no checkpoint.

## Como jogar na Unity

1. Clone o repositório e adicione a pasta do projeto pelo Unity Hub.
2. Abra com a Unity `6000.7.0a4`, a versão indicada em `ProjectSettings/ProjectVersion.txt`.
3. No painel **Project**, abra a cena `Assets/Nyx/Scenes/Manutencao_Interativa.unity` e pressione **Play**.

| Controle | Ação |
| --- | --- |
| **A/D** ou **setas esquerda/direita** | Andar e empurrar caixas. |
| **Espaço** | Pular. Segure para saltar mais alto. |
| **Mouse** | Mirar. Com o lançador: esquerdo abre o portal roxo, direito abre o verde. Com a pistola: esquerdo atira. |
| **E** | Recolher a pistola, pegar/soltar/instalar a célula e interagir com a porta. A munição é recolhida ao encostar. |
| **Q** | Alternar entre a pistola e os portais depois de pegar a pistola. |
| **R** | Recarregar a pistola usando a reserva. |
| **F5** | Reiniciar a fase. |
