# 📘 Guia do Desenvolvedor — Fluxo de CI/CD

Este documento explica **passo a passo** o fluxo completo de trabalho com CI/CD neste projeto.  
Leia com atenção antes de começar a desenvolver.

---

## 📋 Índice

1. [Visão Geral do Fluxo](#1--visão-geral-do-fluxo)
2. [Branches e Responsabilidades](#2--branches-e-responsabilidades)
3. [Passo a Passo — Do Código ao Deploy](#3--passo-a-passo--do-código-ao-deploy)
4. [O que cada Pipeline faz](#4--o-que-cada-pipeline-faz)
5. [Como acompanhar as Pipelines](#5--como-acompanhar-as-pipelines)
6. [Cenários do dia a dia](#6--cenários-do-dia-a-dia)
7. [O que fazer quando a Pipeline falha](#7--o-que-fazer-quando-a-pipeline-falha)
8. [Regras importantes](#8--regras-importantes)
9. [Diagrama Completo](#9--diagrama-completo)

---

## 1. 🎯 Visão Geral do Fluxo

```
 Você está aqui
      │
      ▼
 ┌──────────┐     PR aberto      ┌──────────┐     PR mergeado     ┌──────────┐
 │ feature/  │ ──────────────────▶│ develop  │ ────────────────── ▶│   main   │
 │   xxx     │    CI roda ✅       │          │   Notifica 📢       │          │
 └──────────┘                     └──────────┘                     └──────────┘
                                                   │                     │
                                                   │ PR aberto           │ PR mergeado
                                                   │                     │
                                                   ▼                     ▼
                                              (nenhuma              CD PROD 🚀
                                               pipeline)           Build + Push ECR
                                                                   + Deploy EC2
```

**Resumo:**
- Commit na feature → **nada acontece**
- PR para develop → **CI roda** (build + testes)
- Merge na develop → **notifica** que precisa abrir PR pra main
- PR de develop para main → **nada acontece** (CI já rodou antes)
- Merge na main → **CD roda** (build imagem + push ECR + deploy na EC2)

---

## 2. 🌳 Branches e Responsabilidades

| Branch | Propósito | Quem pode mergear | Proteção |
|--------|-----------|-------------------|----------|
| `feature/*` | Desenvolvimento de funcionalidades | Desenvolvedor | Nenhuma |
| `develop` | Branch de integração / staging | Após aprovação de PR | PR obrigatório + CI passando |
| `main` | Produção | Após aprovação de PR | PR obrigatório |

### Convenção de nomes para branches

```
feature/nome-da-feature     → Nova funcionalidade
fix/descricao-do-bug        → Correção de bug
hotfix/descricao             → Correção urgente em produção
```

---

## 3. 🚶 Passo a Passo — Do Código ao Deploy

### Etapa 1: Criar sua branch

```bash
# Sempre parta da develop atualizada
git checkout develop
git pull origin develop
git checkout -b feature/minha-feature
```

### Etapa 2: Desenvolver e commitar

```bash
# Desenvolva normalmente
# Faça quantos commits precisar
git add .
git commit -m "feat: descrição da alteração"
git push origin feature/minha-feature
```

> ⚠️ **Nenhuma pipeline roda neste momento.** Você pode commitar e pushar à vontade.

### Etapa 3: Abrir Pull Request → develop

1. Vá no GitHub: https://github.com/marcelo-luizz/dotnet-test-repo
2. Clique em **"Compare & pull request"** (ou vá em Pull Requests → New)
3. Configure:
   - **base:** `develop` ← **compare:** `feature/minha-feature`
   - Título descritivo
   - Descrição do que foi feito
4. Clique em **"Create pull request"**

> ✅ **Neste momento a pipeline de CI é acionada automaticamente.**

### Etapa 4: Aguardar o CI

A pipeline de CI vai:
1. ✅ Compilar o projeto
2. ✅ Rodar todos os testes
3. ✅ Validar o build do Docker

Você verá o status diretamente no PR:

```
 ✅ CI - Build & Test — All checks have passed
```

ou

```
 ❌ CI - Build & Test — Some checks failed
```

> ❌ Se falhou, **corrija o código**, faça push e o CI roda novamente automaticamente.

### Etapa 5: Code Review + Merge na develop

1. Solicite review de um colega
2. Após aprovação, clique em **"Merge pull request"**
3. Pode usar **Squash and merge** para manter o histórico limpo

> 📢 Ao mergear, o workflow `cd-dev.yml` roda e exibe uma mensagem:  
> *"Abra um PR de develop → main para acionar o deploy em produção."*

### Etapa 6: Abrir Pull Request → main

1. Vá no GitHub (o link aparece no log da pipeline anterior)
2. Crie um PR:
   - **base:** `main` ← **compare:** `develop`
   - Título: ex. `Deploy - Sprint 12` ou `Release v1.2.0`
3. Clique em **"Create pull request"**

> ⚠️ Nenhuma pipeline roda ao abrir esse PR (o CI já validou na develop).

### Etapa 7: Merge na main → Deploy automático 🚀

1. Revise o PR e clique em **"Merge pull request"**
2. **O deploy é acionado automaticamente!**

A pipeline de CD vai:
1. 🐳 Build da imagem Docker
2. 📦 Push da imagem para o AWS ECR
3. 🔍 Encontrar a instância EC2 pelas tags
4. 🚀 Enviar comandos via SSM para a EC2:
   - Sincronizar variáveis de ambiente (`.env`)
   - Pull da nova imagem
   - Parar containers atuais
   - Subir novos containers
   - Rodar health check

---

## 4. ⚙️ O que cada Pipeline faz

### Pipeline: `ci.yml` — CI Build & Test

| Quando roda | PR aberto/atualizado para `develop` |
|-------------|-------------------------------------|
| O que faz   | Restore → Build → Test → Docker build (validação) |
| Duração     | ~2-3 minutos |
| Se falhar   | ❌ Bloqueia o merge do PR |

**Steps detalhados:**
```
1. 📥 Checkout do código
2. 🛠️ Setup .NET 8.0 SDK
3. 📦 dotnet restore
4. 🔨 dotnet build --configuration Release
5. 🧪 dotnet test (resultados salvos como artefato)
6. 🐳 docker build (valida que o Dockerfile funciona)
```

### Pipeline: `cd-dev.yml` — CD DEV (Notificação)

| Quando roda | PR mergeado na `develop` |
|-------------|--------------------------|
| O que faz   | Exibe mensagem para o dev abrir PR → main |
| Duração     | ~10 segundos |

> 📌 No futuro, quando houver ambiente DEV, esse workflow fará deploy automaticamente. A lógica já está pronta (comentada no arquivo).

### Pipeline: `cd-prod.yml` — CD PROD (Deploy)

| Quando roda | PR mergeado na `main` |
|-------------|------------------------|
| O que faz   | Build + Push ECR + Deploy EC2 via SSM |
| Duração     | ~5-8 minutos |
| Se falhar   | ❌ Imagem pode ter sido enviada ao ECR mas deploy na EC2 falhou |

**Steps detalhados:**
```
1. 📥 Checkout do código
2. 🔑 Configura credenciais AWS
3. 🔐 Login no Amazon ECR
4. 🐳 docker build (com tag sha + latest)
5. 📦 docker push para o ECR
6. 🔍 Busca instância EC2 por tags (AppName=api, Environment=dev)
7. 🚀 Envia script de deploy via SSM:
     └─ cd /opt/app
     └─ ./sync-env.sh (atualiza .env)
     └─ Login no ECR
     └─ docker-compose pull
     └─ docker-compose down
     └─ docker image prune -f
     └─ docker-compose up -d
     └─ Health check (curl /api/health)
8. ✅ ou ❌ Exibe output e status
```

### Pipeline: `rollback.yml` — Rollback em Produção (Manual)

| Quando roda | Manualmente (`workflow_dispatch`) |
|-------------|-----------------------------------|
| O que faz   | Rollback para versão anterior ou específica |
| Duração     | ~3-5 minutos |
| Parâmetros  | SHA do commit + confirmação obrigatória |

**Como usar:**
1. 📋 Execute `LIST DEPLOYMENTS` primeiro para ver versões disponíveis
2. 🔄 Vá em `Actions → ROLLBACK → Run workflow`  
3. ✍️ Informe o SHA (7 caracteres) da versão desejada
4. ⚠️ Digite "CONFIRMO" obrigatoriamente
5. 🚀 Execute o workflow

**Steps detalhados:**
```
1. 🔍 Validação: Verifica se imagem existe no ECR
2. 🐳 Deploy: Substitui imagem no docker-compose via SSM  
3. 🏥 Health Check: Testa endpoint /api/health
4. 📢 Notificação: Informa sucesso/falha
5. 🚨 Recuperação: Se falhar, tenta voltar versão anterior
```

### Pipeline: `list-deployments.yml` — Consultar Versões (Manual)

| Quando roda | Manualmente (`workflow_dispatch`) |
|-------------|-----------------------------------|
| O que faz   | Lista commits e imagens disponíveis |
| Duração     | ~30 segundos |
| Útil para   | Identificar SHA correto para rollback |

**Como usar:**
1. 📋 Vá em `Actions → LIST DEPLOYMENTS → Run workflow`
2. 🎯 Escolha ambiente (dev/prod) 
3. 🔢 Defina quantas versões mostrar (padrão: 10)
4. 📊 Veja relatório completo com commits, imagens ECR e status EC2

---

## 5. 👀 Como acompanhar as Pipelines

### No Pull Request
O status aparece automaticamente no PR. Ícones:
- 🟡 Pipeline rodando
- ✅ Pipeline passou
- ❌ Pipeline falhou

### Na aba Actions
1. Vá em https://github.com/marcelo-luizz/dotnet-test-repo/actions
2. Selecione o workflow desejado
3. Clique na execução para ver os logs

### Artefatos
Após cada execução do CI, os resultados dos testes ficam disponíveis como artefato para download na aba "Actions" → execução → "Artifacts".

---

## 6. 📌 Cenários do dia a dia

### "Fiz push na minha feature e nada aconteceu"
✅ **Correto!** Pipelines só rodam quando tem PR para `develop` ou merge na `main`.

### "O CI falhou no meu PR"
1. Clique em **"Details"** ao lado do check que falhou
2. Veja qual step falhou:
   - **Build falhou** → erro de compilação no código
   - **Test falhou** → algum teste quebrou
   - **Docker build falhou** → problema no Dockerfile
3. Corrija o código, faça push → CI roda novamente

### "Mergeei na develop, e agora?"
O workflow `cd-dev.yml` vai rodar e mostrar uma mensagem no log:
> *"👉 Abra um PR de develop → main para acionar o deploy."*

Siga o link e crie o PR.

### "Mergeei na main mas o deploy falhou"
1. Vá em **Actions** → **CD PROD** → clique na execução com ❌
2. Veja o step que falhou:
   - **Build/Push** → problema na imagem ou credenciais
   - **Get EC2 Instance ID** → instância não encontrada (verificar tags)
   - **Deploy via SSM** → algum comando falhou na EC2

O output do SSM mostra exatamente o que aconteceu na EC2.

### "Preciso fazer um hotfix urgente"
```bash
git checkout main
git pull origin main
git checkout -b hotfix/descricao
# ... faz o fix ...
git push origin hotfix/descricao
# Abra PR direto para main (sem passar pela develop)
# Após merge, o CD roda e faz deploy
# Depois faça merge da main na develop para sincronizar
```

---

## 7. 🔥 O que fazer quando a Pipeline falha

### Erros comuns no CI

| Erro | Causa | Solução |
|------|-------|---------|
| `Build failed` | Erro de compilação | Verifique os erros de compilação no log |
| `Test failed` | Teste quebrando | Rode `dotnet test` localmente e corrija |
| `Docker build failed` | Dockerfile com problema | Rode `docker build .` localmente |

### Erros comuns no CD

| Erro | Causa | Solução |
|------|-------|---------|
| `Login to ECR failed` | Credenciais AWS inválidas | Verifique os secrets no GitHub |
| `Push failed` | Repositório ECR não existe | Crie o repositório no ECR |
| `Instance not found` | Tags da EC2 incorretas | Verifique tags `AppName=api`, `Environment=dev` |
| `docker-compose not found` | Docker Compose não instalado | Instale na EC2 |
| `Health check failed` | App demorou para subir | Pode ser normal, verifique os logs na EC2 |

### Como verificar na EC2

Conecte via Session Manager (Console AWS → EC2 → Connect) e rode:
```bash
cd /opt/app
docker-compose ps          # Ver status dos containers
docker-compose logs -f     # Ver logs em tempo real
cat .env                   # Verificar variáveis de ambiente
```

---

## 8. 📏 Regras importantes

### ✅ Faça sempre
- Rode `dotnet test` **antes** de abrir o PR
- Rode `docker build .` localmente se mexeu no Dockerfile
- Escreva descrições claras nos PRs
- Aguarde o CI passar antes de pedir review

### ❌ Nunca faça
- Push direto na `develop` ou `main` (sempre via PR)
- Merge com CI falhando
- Ignorar o health check falhando após deploy

### 💡 Boas práticas
- Mantenha PRs pequenos e focados
- Use commits semânticos: `feat:`, `fix:`, `refactor:`, `docs:`
- Atualize sua branch com develop antes de abrir PR:
  ```bash
  git checkout feature/minha-feature
  git pull origin develop
  # Resolva conflitos se houver
  ```

---

## 9. 🗺️ Diagrama Completo

```
  DESENVOLVEDOR                     GITHUB                           AWS
  ─────────────                     ──────                           ───

  ┌─────────────┐
  │ Cria branch │
  │ feature/xxx │
  └──────┬──────┘
         │
         │ git push
         │
         │                    ┌─────────────────┐
         │ ──── Abre PR ────▶│   Pull Request   │
         │   feature → dev   │  feature → dev   │
         │                    └────────┬────────┘
         │                             │
         │                             │ trigger
         │                             ▼
         │                    ┌─────────────────┐
         │                    │    ci.yml        │
         │                    │ ┌─────────────┐  │
         │                    │ │ 🔨 Build    │  │
         │                    │ │ 🧪 Test     │  │
         │                    │ │ 🐳 Docker   │  │
         │                    │ └─────────────┘  │
         │                    └────────┬─────────┘
         │                             │
         │                        ✅ ou ❌
         │                             │
         │                    ┌────────▼────────┐
         │ ◀── Code Review ──│   PR aprovado    │
         │                    │   Merge → dev    │
         │                    └────────┬────────┘
         │                             │
         │                             │ trigger
         │                             ▼
         │                    ┌─────────────────┐
         │                    │   cd-dev.yml     │
         │                    │   📢 Notifica:  │
         │                    │  "Abra PR pra   │
         │                    │     main"        │
         │                    └────────┬────────┘
         │                             │
         │                    ┌────────▼────────┐
         │ ──── Abre PR ────▶│  Pull Request    │
         │   dev → main      │  develop → main  │
         │                    └────────┬────────┘
         │                             │
         │                    ┌────────▼────────┐
         │ ◀── Review ───────│   PR aprovado    │
         │                    │  Merge → main    │
         │                    └────────┬────────┘
         │                             │
         │                             │ trigger
         │                             ▼
         │                    ┌─────────────────┐
         │                    │   cd-prod.yml    │
         │                    │ ┌─────────────┐  │         ┌──────────────┐
         │                    │ │ 🐳 Build    │──│────────▶│  Amazon ECR   │
         │                    │ │ 📦 Push     │  │         │  (imagem)    │
         │                    │ └─────────────┘  │         └──────────────┘
         │                    │ ┌─────────────┐  │         ┌──────────────┐
         │                    │ │ 🔍 Find EC2 │  │         │   EC2 (SSM)  │
         │                    │ │ 🚀 Deploy   │──│────────▶│  /opt/app    │
         │                    │ │    via SSM   │  │         │  sync-env    │
         │                    │ └─────────────┘  │         │  compose up  │
         │                    └─────────────────┘         └──────────────┘
         │
         │                                                 ┌──────────────┐
         └─────────────── curl /api/health ──────────────▶│  API online!  │
                                                           │  :8080       │
                                                           └──────────────┘
```

---

## ❓ Dúvidas Frequentes

**P: Posso fazer push direto na develop?**  
R: Não. Sempre via Pull Request.

**P: Se eu fizer mais commits no PR, o CI roda de novo?**  
R: Sim, automaticamente a cada push na branch do PR.

**P: Quanto tempo demora o deploy completo?**  
R: CI ~2-3min + CD ~5-8min = **~10 minutos** do merge na main até estar no ar.

**P: Como sei se o deploy funcionou?**  
R: Vá em Actions → CD PROD e veja se ficou ✅. O health check no final confirma se a API está respondendo.

**P: O que é o `sync-env.sh`?**  
R: É um script na EC2 que puxa variáveis de ambiente do AWS SSM Parameter Store e gera o arquivo `.env` usado pelo `docker-compose.yml`.

**P: Posso fazer rollback?**  
R: **Sim! Use os workflows do GitHub Actions:**
- 📋 **LIST DEPLOYMENTS:** Para ver versões disponíveis (`Actions → LIST DEPLOYMENTS`)
- 🔄 **ROLLBACK:** Para fazer rollback seguro (`Actions → ROLLBACK`)

Os workflows fazem:
- ✅ Validação automática da imagem no ECR
- 🐳 Deploy seguro da versão escolhida  
- 🏥 Health check automático
- 📢 Notificações de status
- ⚠️ Tentativa de recuperação em caso de falha

**Manual alternativo:** Na EC2, altere o `docker-compose.yml` para apontar para a tag anterior e rode `docker-compose up -d`.
