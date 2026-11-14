SELECT id,
       created_at,
       created_by_user_id,
       modified_at,
       modified_by_user_id,
       library_id,
       storage_path,
       external_id,
       external_url,
       name,
       refreshed_at,
       subscribed_at
FROM media.playlists
