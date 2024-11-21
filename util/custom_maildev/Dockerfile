# Base
FROM node:16-alpine as base
ENV NODE_ENV production

# Build
FROM base as build
WORKDIR /root
COPY package*.json ./
RUN npm install \
  && npm prune \
  && npm cache clean --force

# Prod
FROM base as prod

LABEL com.hitachi.product="bravura_vault"

USER node
WORKDIR /home/node
COPY --chown=node:node . /home/node
COPY --chown=node:node --from=build /root/node_modules /home/node/node_modules
EXPOSE 1080 1025
ENV MAILDEV_WEB_PORT 1080
ENV MAILDEV_SMTP_PORT 1025
RUN chmod +x bin/maildev
ENTRYPOINT ["bin/maildev"]
HEALTHCHECK --interval=10s --timeout=5s --retries=3 \
  CMD wget -O - http://127.0.0.1:1080/healthz || exit 1