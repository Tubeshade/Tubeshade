CREATE FUNCTION text_array_to_string(text[], text)
    RETURNS text
    LANGUAGE sql
    IMMUTABLE AS
$$
SELECT array_to_string($1, $2)
$$;

ALTER TABLE media.videos
    ADD COLUMN searchable_index_value tsvector NOT NULL GENERATED ALWAYS AS (
        to_tsvector(
                'english',
                videos.name || ' ' || videos.description || ' ' || text_array_to_string(videos.tags, ' '))
        ) STORED;

CREATE INDEX ON media.videos USING GIN (searchable_index_value);

ANALYSE;
