package com.example.shooter.game.world;

import com.example.shooter.exception.ApiException;
import com.example.shooter.exception.ErrorCode;
import com.example.shooter.game.player.Player;
import com.example.shooter.game.player.PlayerRepository;
import com.example.shooter.game.player.PlayerRepresentation;
import com.example.shooter.game.player.PlayerRole;
import com.example.shooter.jwt.UnityServerTokenProvider;
import com.example.shooter.user.User;
import com.example.shooter.user.UserRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.Instant;
import java.util.*;
import java.util.stream.Collectors;

@Service
@Slf4j
public class WorldService {

    private final UserRepository userRepository;
    private final WorldRepository worldRepository;
    private final PlayerRepository playerRepository;
    private final UnityServerTokenProvider unityServerTokenProvider;
    private final WorldUnityHookService worldUnityHookService;

    public WorldService(UserRepository userRepository, WorldRepository worldRepository, PlayerRepository playerRepository, UnityServerTokenProvider unityServerTokenProvider, WorldUnityHookService worldUnityHookService) {
        this.userRepository = userRepository;
        this.worldRepository = worldRepository;
        this.playerRepository = playerRepository;
        this.unityServerTokenProvider = unityServerTokenProvider;
        this.worldUnityHookService = worldUnityHookService;
    }

    @Transactional
    public WorldRepresentation create(Long userId, CreateWorldRequest request) {
        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));

        Long now = Instant.now().getEpochSecond();

        World world = new World();
        world.setName(request.getName());
        world.setCreatedAt(now);
        world.setAccessedAt(now);
        world.setJoinPolicy(request.getJoinPolicy());
        world.setPlayers(1L);
        world = worldRepository.save(world);

        Player player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setRole(PlayerRole.CREATOR);
        player.setMemberSince(now);
        player.setLastSeen(now);
        player.setBlacklisted(false);
        player = playerRepository.save(player);

        log.info("user {} created new world {} player {}", userId, world.getId(), player.getId());

        return new WorldRepresentation(
                world,
                List.of(new PlayerRepresentation(player))
        );
    }

    @Transactional
    public WorldJoinResponse join(Long userId, UUID worldId) {
        User user = userRepository.findById(userId).orElseThrow(() -> new ApiException(ErrorCode.NOT_AUTHENTICATED));
        Long now = Instant.now().getEpochSecond();

        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElseThrow(() -> new ApiException(ErrorCode.WORLD_NOT_FOUND));
        world.setAccessedAt(now);
        worldRepository.save(world);

        Player player = playerRepository.findByUserIdAndWorldIdForPessimisticWrite(userId, worldId).orElse(null);

        if (player != null) {
            if (player.getBlacklisted()) {
                log.info("user {} tried to come back to the world {} where he is in the blacklist", userId, worldId);
                throw new ApiException(ErrorCode.BLACKLISTED);
            }
            player.setLastSeen(now);
            playerRepository.save(player);
            log.info("user {} came back to world {} as player {}", userId, worldId, player.getId());
            return new WorldJoinResponse(
                    unityServerTokenProvider.generateToken(userId + ":" + worldId)
            );
        }

        if (world.getJoinPolicy() != WorldJoinPolicy.EVERYONE) {
            log.info("user {} couldn't join world {}: closen world join policy", userId, worldId);
            throw new ApiException(ErrorCode.WORLD_DOES_NOT_ACCEPT_NEW_MEMBERS);
        }

        player = new Player();
        player.setWorld(world);
        player.setUser(user);
        player.setRole(PlayerRole.MEMBER);
        player.setMemberSince(now);
        player.setLastSeen(now);
        player.setBlacklisted(false);
        player = playerRepository.save(player);

        world.setPlayers(world.getPlayers() + 1);
        worldRepository.save(world);

        log.info("user {} joined world {} as player {} for the first time!", userId, worldId, player.getId());
        return new WorldJoinResponse(
                unityServerTokenProvider.generateToken(userId + ":" + worldId)
        );
    }

    public Map<String, String> kick(Long userId, UUID worldId, Long targetId) {
        if (Objects.equals(userId, targetId)) {
            log.info("user {} tried to kick themselves from the world {}", userId, worldId);
            throw new ApiException(ErrorCode.CANT_SELF_KICK);
        }

        Player player = playerRepository.findByUserIdAndWorldId(userId, worldId).orElseThrow(() -> new ApiException(ErrorCode.NOT_A_MEMBER));
        Player target = playerRepository.findByUserIdAndWorldId(targetId, worldId).orElseThrow(() -> new ApiException(ErrorCode.PLAYER_NOT_FOUND));

        if (player.getRole().ordinal() <= target.getRole().ordinal()) {
            log.info("user {} {} tried to kick target {} {} from the world {}", userId, player.getRole(), targetId, target.getRole(), worldId);
            throw new ApiException(ErrorCode.CANT_KICK_THIS_USER);
        }

        log.info("user {} kicked target {} from the world {}", userId, targetId, worldId);
        worldUnityHookService.registerTask(new UnityHook(targetId, worldId));
        return Map.of("status", "ok");
    }

    @Transactional
    public Map<String, String> blacklist(Long userId, UUID worldId, Long targetId) {
        if (Objects.equals(userId, targetId)) {
            log.info("user {} tried to blacklist themselves in world {}", userId, worldId);
            throw new ApiException(ErrorCode.CANT_SELF_BLACKLIST);
        }
        requireCreator(userId, worldId);

        Player target = playerRepository.findByUserIdAndWorldIdForPessimisticWrite(targetId, worldId).orElseThrow(() -> new ApiException(ErrorCode.PLAYER_NOT_FOUND));
        if (target.getBlacklisted()) {
            log.info("user {} tried to blacklist already blacklisted target {} in the world {}", userId, targetId, worldId);
            throw new ApiException(ErrorCode.ALREADY_BLACKLISTED);
        }
        target.setBlacklisted(true);
        playerRepository.save(target);
        worldUnityHookService.registerTask(new UnityHook(targetId, worldId));
        log.info("user {} blacklisted target {} in the world {}", userId, targetId, worldId);
        return Map.of("status", "ok");
    }

    @Transactional
    public Map<String, String> unblacklist(Long userId, UUID worldId, Long targetId) {
        requireCreator(userId, worldId);

        Player target = playerRepository.findByUserIdAndWorldIdForPessimisticWrite(targetId, worldId).orElseThrow(() -> new ApiException(ErrorCode.PLAYER_NOT_FOUND));
        if (!target.getBlacklisted()) {
            log.info("user {} tried to unblacklist not blacklisted target {} in the world {}", userId, targetId, worldId);
            throw new ApiException(ErrorCode.NOT_BLACKLISTED);
        }
        target.setBlacklisted(false);
        playerRepository.save(target);
        log.info("user {} unblacklisted target {} in the world {}", userId, targetId, worldId);
        return Map.of("status", "ok");
    }

    @Transactional
    public WorldRepresentation patch(Long userId, UUID worldId, PatchWorldRequest request) {
        if (request.getName() == null && request.getJoinPolicy() == null) {
            throw new ApiException(ErrorCode.EMPTY_REQUEST);
        }

        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElseThrow(() -> new ApiException(ErrorCode.WORLD_NOT_FOUND));
        requireCreator(userId, world.getId());

        if (request.getName() != null) world.setName(request.getName());
        if (request.getJoinPolicy() != null) world.setJoinPolicy(request.getJoinPolicy());
        world = worldRepository.save(world);

        log.info("user {} patched world {}", userId, worldId);

        List<PlayerRepresentation> players = playerRepository.findAllByWorldIdsWithWorldsAndUsers(Set.of(worldId))
                .stream()
                .map(PlayerRepresentation::new)
                .toList();
        return new WorldRepresentation(world, players);
    }

    @Transactional
    public void delete(Long userId, UUID worldId) {
        requireCreator(userId, worldId);

        worldRepository.deleteById(worldId);
        log.info("user {} deleted world {}", userId, worldId);
        worldUnityHookService.registerTask(new UnityHook(null, worldId));
    }

    private void requireCreator(Long userId, UUID worldId) {
        Player player = playerRepository.findByUserIdAndWorldId(userId, worldId).orElse(null);
        if (player == null) {
            log.info("user {} tried to manage the world {} while not being a member", userId, worldId);
            throw new ApiException(ErrorCode.NOT_A_MEMBER);
        }
        if (player.getRole().ordinal() < PlayerRole.CREATOR.ordinal()) {
            log.info("user {} tried to manage the world {} while not being a creator", userId, worldId);
            throw new ApiException(ErrorCode.NOT_A_CREATOR);
        }
    }

    public List<WorldRepresentation> get(Long userId, PlayerRole playerRole, Integer page, Integer size) {
        Pageable pageable = PageRequest.of(page, size);

        List<World> worlds = worldRepository.findByUserIdAndPlayerRoleOrderedByLastSeen(userId, playerRole, pageable);
        Set<UUID> worldIds = worlds.stream().map(World::getId).collect(Collectors.toSet());

        List<Player> players = playerRepository.findAllByWorldIdsWithWorldsAndUsers(worldIds);

        Map<UUID, WorldRepresentation> resultMap = new LinkedHashMap<>();
        for (World w : worlds) {
            resultMap.put(w.getId(), new WorldRepresentation(w, new ArrayList<>()));
        }
        for (Player p : players) {
            resultMap.get(p.getWorld().getId()).getPlayers().add(new PlayerRepresentation(p));
        }
        return resultMap.values().stream().toList();
    }
}
