package com.example.shooter.game.world;

import com.example.shooter.game.player.PlayerRole;
import jakarta.persistence.LockModeType;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

public interface WorldRepository extends JpaRepository<World, UUID> {
    @Query("SELECT COUNT(w) FROM World w WHERE w.createdAt >= ?1")
    long countCreatedSince(Long timestamp);

    @Query("SELECT COUNT(w) FROM World w WHERE w.accessedAt >= ?1")
    long countAccessedSince(Long timestamp);

    @Query("SELECT w FROM World w WHERE (?1 IS NULL OR w.id in ?1) AND (?2 IS NULL OR w.visibilityPolicy = ?2) AND (?3 IS NULL OR w.joinPolicy = ?3) ORDER BY w.accessedAt DESC, w.id")
    List<World> findByWorldIdsAndVisibilityPolicyAccessedAtOrder(Set<UUID> worldIds, WorldVisibilityPolicy visibilityPolicy, WorldJoinPolicy joinPolicy, Pageable pageable);

    @Lock(LockModeType.PESSIMISTIC_WRITE)
    @Query("SELECT w FROM World w WHERE w.id = ?1")
    Optional<World> findByIdForPessimisticWrite(UUID worldId);

    @Query("SELECT w.id FROM World w WHERE NOT EXISTS (SELECT 1 FROM Player p WHERE p.world = w AND p.role = ?1)")
    List<UUID> findWorldIdsWithoutRole(PlayerRole playerRole);
}
