FROM node:22-alpine AS build
RUN corepack enable && corepack prepare pnpm@9.15.9 --activate
WORKDIR /workspace
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml turbo.json tsconfig.base.json ./
COPY apps/web-pos apps/web-pos
COPY apps/web-kds apps/web-kds
COPY apps/web-menu apps/web-menu
COPY apps/web-admin apps/web-admin
COPY packages packages
RUN pnpm install --frozen-lockfile
RUN pnpm --filter @db/ui... build \
 && pnpm --filter @db/web-pos build --base=/pos/ \
 && pnpm --filter @db/web-kds build --base=/kds/ \
 && pnpm --filter @db/web-menu build --base=/menu/ \
 && pnpm --filter @db/web-admin build --base=/admin/

FROM nginx:1.27-alpine
COPY infra/edge/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /workspace/apps/web-pos/dist /usr/share/nginx/html/pos
COPY --from=build /workspace/apps/web-kds/dist /usr/share/nginx/html/kds
COPY --from=build /workspace/apps/web-menu/dist /usr/share/nginx/html/menu
COPY --from=build /workspace/apps/web-admin/dist /usr/share/nginx/html/admin
