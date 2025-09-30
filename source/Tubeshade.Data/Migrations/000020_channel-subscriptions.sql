CREATE TYPE media.subscription_status AS ENUM ('subscription_pending', 'subscribed', 'unsubscription_pending');

CREATE TABLE media.channel_subscriptions
(
    id                  uuid                                  NOT NULL PRIMARY KEY REFERENCES media.channels (id) NOT DEFERRABLE,
    created_at          timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by_user_id  uuid                                  NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,
    modified_at         timestamptz DEFAULT CURRENT_TIMESTAMP NOT NULL,
    modified_by_user_id uuid                                  NOT NULL REFERENCES identity.users (id) NOT DEFERRABLE,

    status              media.subscription_status             NOT NULL,
    callback            text                                  NOT NULL,
    topic               text                                  NOT NULL,
    expires_at          timestamptz                           NULL,
    verify_token        text                                  NULL,
    secret              text                                  NULL
);
