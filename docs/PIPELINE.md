# 🔄 Pipeline CI/CD

## Visão Geral

A pipeline é dividida em **2 workflows** separados:

```
                          feature/xxx
                              │
                              │  push (nada acontece)
                              │
                              ▼
                     Abre PR → develop
                              │
                 ┌────────────┤
                 ▼            │
          ┌────────────┐      │
          │  ci.yml    │      │
          │ 🔨 Build   │      │
          │ 🧪 Test    │      │
          │ 🐳 Docker  │      │
          └────────────┘      │
                              │  PR mergeado
                              ▼
                       ┌────────────┐
                       │  cd.yml    │
                       │ 🔨 Build   │
                       │ 🧪 Test    │
                       │ 🐳 ECR     │
                       │ 🚀 Deploy  │
                       └────────────┘
```

---

## Workflow 1: CI (`ci.yml`)

**Arquivo:** `.github/workflows/ci.yml`

**Aciona quando:**
- Push na branch `develop`
- Pull Request aberto para `develop` (ex: `feature/xxx` → `develop`)

**Steps:**
1. Checkout do código
2. Setup .NET 8 SDK
3. Restore das dependências
4. Build em modo Release
5. Execução dos testes (com upload dos resultados)
6. Validação do build Docker

> ⚠️ Push direto em branches `feature/*` **não aciona** a pipeline. Só quando abre PR para `develop`.

---

## Workflow 2: CD (`cd.yml`)

**Arquivo:** `.github/workflows/cd.yml`

**Aciona quando:**
- Pull Request é **mergeado** na `develop`

> PRs fechados sem merge **não acionam** o deploy.

**Steps:**
1. Checkout do código
2. Build e testes (validação final)
3. Login no AWS ECR
4. Build e push da imagem Docker para o ECR
5. SSH na EC2 para:
   - Pull da nova imagem do ECR
   - Stop/Remove do container antigo
   - Start do novo container
   - Health check

---

## Fluxo de Trabalho

1. Criar uma branch `feature/xxx` a partir de `develop`
2. Fazer as alterações e commitar (nada acontece)
3. Abrir um Pull Request de `feature/xxx` → `develop`
4. **CI roda automaticamente** (build + testes)
5. Code review e aprovação
6. Merge do PR
7. **CD roda automaticamente** (build + push ECR + deploy EC2)

---

## Secrets Necessários

| Secret                  | Exemplo / Descrição                       |
|-------------------------|-------------------------------------------|
| `AWS_ACCESS_KEY_ID`     | Access Key do IAM user                    |
| `AWS_SECRET_ACCESS_KEY` | Secret Key do IAM user                    |
| `EC2_HOST`              | `54.123.45.67`                            |
| `EC2_USER`              | `ubuntu`                                  |
| `EC2_SSH_KEY`           | Conteúdo da chave `.pem` da EC2           |

### Como adicionar os Secrets

1. Vá no repositório do GitHub
2. `Settings` → `Secrets and variables` → `Actions`
3. Clique em `New repository secret`
4. Adicione cada secret listado acima

### Obtendo a chave SSH

```bash
# Copie o conteúdo da chave .pem
cat ~/.ssh/sua-chave-ec2.pem
```

Cole o conteúdo completo (incluindo `-----BEGIN RSA PRIVATE KEY-----` e `-----END RSA PRIVATE KEY-----`) como valor do secret `EC2_SSH_KEY`.

## Troubleshooting

### Build falhou
- Verifique se o .NET SDK 8.0 está configurado corretamente
- Veja os logs de teste no artefato `test-results`

### Deploy falhou
- Verifique se os secrets estão configurados corretamente
- Confirme que a EC2 tem Docker instalado
- Confirme que a porta 8080 está liberada no Security Group
- Verifique se a chave SSH está correta

### Health check falhou após deploy
- O container pode demorar alguns segundos para iniciar
- Verifique os logs: `docker logs sample-api`
