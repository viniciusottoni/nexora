# ADR-018 — CI/CD

Status: Aceito

## Contexto

O AWAKEN terá app Flutter e backend .NET no mesmo repositório. É necessário validar código, testes e build antes de publicação.

## Decisão

Usar GitHub Actions como ferramenta oficial de CI/CD.

## Implementação

- Criar workflow para Flutter com análise, testes e build Android.
- Criar workflow para backend com restore, build e testes.
- Separar validação de pull request e release.
- Gerar artefato Android para teste interno.
- Manter variáveis sensíveis fora do código.
- Executar deploy do backend por workflow controlado.

## Consequências

A entrega fica mais previsível e reduz risco de regressão na branch principal.

## Critérios de aceite

- A validação roda ao abrir PR.
- O backend compila no pipeline.
- O app compila no pipeline.
- O release gera artefato Android.
